using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace Reborn
{
    // Explicit development connection. Never falls back to local training on network failure.
    public sealed class ServerTrainingClient:MonoBehaviour
    {
        [Serializable] sealed class Session { public string token; public AoTrainingState character; }
        [Serializable] sealed class Reply { public string reason; public AoTrainingState character; }
        [Serializable] sealed class TrainRequest { public int statId; public long revision; }
        DistrictGame game;string endpoint,token;long revision;bool busy;
        IEnumerator Start()
        {
            game=GetComponent<DistrictGame>();
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-server");
            if(at<0||at+1>=args.Length||!Uri.TryCreate(args[at+1],UriKind.Absolute,out var uri)||!uri.IsLoopback||uri.Scheme!="http")
            {game.Log("Development server requires a loopback HTTP address.");yield break;}
            endpoint=uri.GetLeftPart(UriPartial.Authority);
            using(var request=Request("/sessions","{}"))
            {
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){game.Log("Server connection failed. Training is unavailable.");yield break;}
                var session=JsonUtility.FromJson<Session>(request.downloadHandler.text);
                token=session.token;Apply(session.character);game.Log("Connected to character server. Skills and IP are server controlled.");
            }
            if(Array.IndexOf(args,"-qaServer")>=0)
            {
                yield return SendTraining(116);
                bool ok=revision==1&&game.Build.ip==1495&&game.Build.investments[116]==1;
                Debug.Log(ok?"SERVER_CLIENT_QA_OK":"SERVER_CLIENT_QA_FAILED");
                Application.Quit(ok?0:1);
            }
        }
        UnityWebRequest Request(string path,string body)
        {
            var r=new UnityWebRequest(endpoint+path,"POST");r.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            r.downloadHandler=new DownloadHandlerBuffer();r.SetRequestHeader("Content-Type","application/json");r.timeout=10;
            if(token!=null)r.SetRequestHeader("Authorization","Bearer "+token);return r;
        }
        void Apply(AoTrainingState state)
        {
            if(state==null||state.investments==null||state.investments.Length!=169)throw new InvalidOperationException("Invalid server character");
            revision=state.revision;game.Build.level=state.level;game.Build.ip=state.ip;game.Build.breed=state.breed;
            game.Build.profession=state.profession;game.Build.investments=state.investments;
        }
        public void Train(int stat)
        {
            if(game==null)return;
            if(token==null||busy){game.Log("Waiting for character server.");return;}
            StartCoroutine(SendTraining(stat));
        }
        IEnumerator SendTraining(int stat)
        {
            busy=true;
            using(var request=Request("/character/train",JsonUtility.ToJson(new TrainRequest{statId=stat,revision=revision})))
            {
                yield return request.SendWebRequest();
                busy=false;
                if(request.responseCode==200||request.responseCode==400||request.responseCode==409)
                {
                    var reply=JsonUtility.FromJson<Reply>(request.downloadHandler.text);
                    if(reply.character!=null)Apply(reply.character);
                    game.Log(reply.reason??"Server rejected the request.");
                }
                else game.Log("Character server unavailable. No local training was applied.");
            }
        }
    }
}
