using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Reborn;
var builder=WebApplication.CreateBuilder(args);
int port=int.Parse(Environment.GetEnvironmentVariable("AO_SERVER_PORT")??"5077");
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=4096);
builder.Services.ConfigureHttpJsonOptions(o=>{o.SerializerOptions.IncludeFields=true;o.SerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow;});
builder.Services.AddRateLimiter(o=>{
 o.RejectionStatusCode=429;
 o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(_=>RateLimitPartition.GetFixedWindowLimiter("development-server",_=>new FixedWindowRateLimiterOptions{PermitLimit=100,Window=TimeSpan.FromSeconds(1),QueueLimit=0}));
});
var app=builder.Build();app.UseRateLimiter();
var json=new JsonSerializerOptions{IncludeFields=true};
var rules=JsonSerializer.Deserialize<AoRules>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Data/rules.json")),json)!;
string folder=Path.GetFullPath(Environment.GetEnvironmentVariable("AO_SERVER_DATA")??Path.Combine(AppContext.BaseDirectory,"characters"));Directory.CreateDirectory(folder);
var gate=new SemaphoreSlim(1,1);
string? SessionPath(HttpContext ctx)
{
 string header=ctx.Request.Headers.Authorization.ToString();if(!header.StartsWith("Bearer ",StringComparison.Ordinal))return null;
 string token=header[7..];if(token.Length!=64||token.Any(c=>!char.IsAsciiHexDigit(c)))return null;
 return Path.Combine(folder,Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)))+".json");
}
async Task Save(string path,AoTrainingState state)
{
 string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
 try{await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(state,json));File.Move(temp,path,true);}
 finally{if(File.Exists(temp))File.Delete(temp);}
}
app.MapGet("/health",()=>Results.Ok(new{status="ok",scope="development character training"}));
app.MapPost("/sessions",async()=>{
 string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var state=new AoTrainingState();
 await Save(Path.Combine(folder,Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)))+".json"),state);
 return Results.Ok(new{token,character=state});
});
app.MapGet("/character",async(HttpContext ctx)=>{
 string? path=SessionPath(ctx);if(path==null)return Results.Unauthorized();
 await gate.WaitAsync();try{if(!File.Exists(path))return Results.Unauthorized();return Results.Ok(JsonSerializer.Deserialize<AoTrainingState>(await File.ReadAllTextAsync(path),json));}finally{gate.Release();}
});
app.MapPost("/character/train",async(HttpContext ctx,TrainRequest request)=>{
 string? path=SessionPath(ctx);if(path==null)return Results.Unauthorized();
 await gate.WaitAsync();try{
 if(!File.Exists(path))return Results.Unauthorized();
 var state=JsonSerializer.Deserialize<AoTrainingState>(await File.ReadAllTextAsync(path),json)!;
 if(request.revision!=state.revision)return Results.Conflict(new{reason="Character changed; refresh before retrying.",character=state});
 if(!AoTrainingRules.Train(rules,state,request.statId,out string reason))return Results.BadRequest(new{reason,character=state});
 state.revision++;await Save(path,state);return Results.Ok(new{reason,character=state});
 }finally{gate.Release();}
});
app.Run();
public sealed record TrainRequest(int statId,long revision);
