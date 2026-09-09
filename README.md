# Anarchy Online remake — Unity / Blender

## GitHub-versie

Download de Windows-demo bij [Releases](https://github.com/sjroesink/anarchy/releases). Pak de volledige ZIP uit en start `AnarchyReborn.exe`; de DLL's en datamappen moeten ernaast blijven staan. Dit is een lokale ontwikkelpreview, nog geen volwaardige multiplayergame.

Voor een eigen build: installeer Unity **6000.6.0f1**, open `Unity` via Unity Hub en voer vanuit de projectmap `./Tools/Build.ps1` uit. De Blender-bron en FBX-exports zijn inbegrepen; `-RebuildModels` vereist Blender 5.2. De losse ontwikkelserver start met `dotnet run --project Server/Anarchy.Server.csproj` (.NET 10).

De originele AO-client, lokale `Research`-checkouts, logs, saves en Unity-cache zijn niet opgenomen. De meegeleverde data en testfixtures volstaan voor de demo-build. Onderzoeks- en reconstructiescripts die `Research` lezen vereisen die afzonderlijke lokale bronnen; verwijzingen daarnaar in de ontwikkelnotities zijn geen meegeleverde bestanden. Bronvermeldingen en toepasselijke derdenlicenties staan in `Unity/Assets/StreamingAssets/ThirdParty` en `Tools/licenses`.

Het actieve einddoel is visueel herkenbaar Anarchy Online met **1-op-1 spelregels en content**. De huidige build is een tussenstap en voldoet daar nog niet aan. De eerdere eigen balans en vereenvoudigde vier-skill-build worden vervangen door versiegebonden AO-data. Zie [Docs/FIDELITY.md](Docs/FIDELITY.md) voor de volledige eisen, bewijsstatus en resterende afwijkingen.

## Huidige prioriteit: look, feel en gameplay

De nieuwste visuele speelversie staat in `Builds/HotbarPreview/AnarchyReborn.exe`. De Soldier heeft nu ook aparte Blender-animaties voor zijwaarts en achteruit bewegen. Deze heeft een tweehands schiethouding op het Blender-personage, zichtbaar richten op het doel, terugslag/mondingslicht, een zachter volgende camera en een vernieuwde hemel. Blender-plantenbakken, transitbanken en verweerde bestrating geven de straat meer detail. Het schietpunt is in Blender aan de geweerloop bevestigd. Treffers tonen korte impacts, missers gaan zichtbaar langs de drone en uitgeschakelde drones laten een korte energieontlading achter. Vijanden zijn nu met links klikken of Tab te selecteren; het geselecteerde doel toont ook een naam en gezondheidsbalk boven het model. Met T of Shift+klik open je de doelinformatie. Zwevende witte/rode getallen tonen toegebrachte/ontvangen schade. Q start/stopt het gevecht; Tab begint bij het dichtstbijzijnde doel en Shift+Tab bladert terug. Dit blijft een blockout/prototype; de volledige AO-uitstraling en spelregels zijn nog niet af. Verdere account- en anti-cheatuitbouw volgt later; de bestaande losse skill-server blijft beschikbaar. Zie `Docs/CURRENT-PRIORITIES.md`.

## Spelen en bewerken

Start `Builds/HotbarPreview/AnarchyReborn.exe` met de bijbehorende mappen en DLL's ernaast. Open voor ontwikkeling de map `Unity` in Unity Hub en laad `Assets/Scenes/Borealis.unity`. Versies: Unity **6000.6.0f1**, Blender **5.2.1**.

| Bediening | Actie |
|---|---|
| W/S, A/D, Z/C | Vooruit/achteruit, draaien, zijwaarts bewegen |
| Backspace / spatie | Lopen-rennen wisselen / springen |
| Rechtermuisknop + bewegen / wiel | Camera draaien / zoomen |
| F8 | Eerste- en derdepersoonszicht wisselen |
| U | Skills, equipment, itemdatabase en nanobibliotheek |
| E bij de terminal | Lokale trainingsmissie openen / beloning ophalen |
| Rechtsklik op de terminal | Missievenster openen; slepen blijft camerabediening |
| Tab / Shift+Tab | Volgend / vorig doelwit |
| Q / slot 1 op laag 1 | Auto-aanval aan-uit |
| Shift+1–0 / Y | Snelbalklaag kiezen / balk verbergen |
| Rechtsklik op snelbalkslot | Actie, geleerd nano-programma of eigen inventarisitem toewijzen |
| Sleep een snelkoppeling | Verplaatsen; bezette slots wisselen; Shift+1–0 kiest een andere laag |
| Escape / rechtsklik tijdens slepen | Verplaatsing annuleren |
| T / Shift+klik | Doelinformatie openen |
| 2 | Burst; aanwezig in de brondata, uitvoering nog niet geïmplementeerd |
| 3 | First Aid; er is nog geen bruikbaar healing-item geïmplementeerd |
| 4 | Assault Rifle Expertise; vereist 61 PM, 61 SI, 40 nano en 4 vrije NCU |
| 5 | Total Mirror Shield Mk I; Soldier, 47 TS, 59 MC, 126 nano en 4 NCU |
| F5 / F2 / Escape | Opslaan / HUD verbergen / pauzeren |

De Soldier begint met de Solar-Powered Assault Rifle (AOID 121569). Het volledige skillscherm werkt met afzonderlijke abilities, skillinvesteringen, profession-afhankelijke kosten en ability-trickle-down. Nano-eisen betekenen dat een level-1 Soldier de twee getoonde nanos nog niet kan uitvoeren. Actieve nanos wijzigen nu de gedeelde stats en NCU-bezetting. Expertise verhoogt daarmee ook de zichtbare Assault Rifle-skill en de waarde voor equipment-eisen. Nano Initiative en de Agg/Def-slider beïnvloeden de casttijd; nano-recharge blijft vast. Bewegen onderbreekt een lopende nano; tijdens het casten toont de HUD voortgang en zijn normale wapenschoten geblokkeerd. Opnieuw casten vernieuwt de buff; annuleren kan via de aparte knop bij de actieve nano. Bij een actieve zoekbalk worden letters niet als gamehotkeys uitgevoerd.

## Huidige voortgang

- **626 stat-ID's**, **75 skill/ability-vermeldingen** (waarvan twee historisch), **14 professions** en **4 breeds** in de referentiedata. Alleen Soldier is momenteel speelbaar.
- **33.836 itemvermeldingen** met oorspronkelijke low/high AOID, QL-bereik, naam, flags en slots. Doorzoekbaar in de build; het merendeel mist nog uitvoerbare itemdata en kan niet worden uitgerust.
- **3.375 nanovermeldingen** met AOID, naam, skill-eisen, kosten en nanoline. Dit is geen complete implementatie van hun effecten.
- Sourced starterwapenwaarden, normale PvM-schade met schade-type/AC en minimumgrens voor AR tot 1000, XP-thresholds en de eerste nanocastvoorwaarden/timers. Meerdere combat- en capregels zijn nog niet tegen de oorspronkelijke client bewezen.
- **13 Blender-modellen**, waaronder nieuwe Whompahs, een Grid-terminal, subspace-schotel en lagere habitats. De Grid-terminal is opnieuw gemodelleerd tegen een bekeken AO-screenshot, met de herkenbare sokkel, monitor, blauwe kap en GRID ACCESS-zijbord. Zie `Docs/VISUAL-REFERENCE.md`. De stad is nog een blockout met een eigen indeling, geen exacte Borealis-kaart. Grid en Whompahs hebben nog geen werkende reisroutes.
- Een lokale drone-trainingsmissie als testomgeving. De generatie, drones en beloningen zijn **geen** AO-missiereconstructie.

Het equipmentvenster heeft nu een echte iteminstantie voor het starterwapen, met afleggen, uitrusttijd, slotcontrole en opslag. Zonder uitgerust wapen kun je niet schieten. Zeven exacte itemrecords bevatten uitvoerbare gegevens: het starterwapen, Ti-100X en 2–3 NCU Memory op QL1, 4–7 NCU Memory op QL20, de 6K-X-belt en 64 NCU Memory op QL200, en Battered Leather Body Armor op QL1. Armor-AC vermindert inkomende projectile-schade en neemt af bij onvoldoende abilities (OE); de character-texture voor dit armor-item is nog niet geïmplementeerd. NCU-slots zijn selecteerbaar; grotere inventories zijn doorbladerbaar. Belt/deck-eisen, modifiers en uitrusttijden zijn verbonden: 10 seconden voor de belt en 1 seconde voor memory. Het equipmentvenster ondersteunt deze items wanneer ze in bezit zijn. Verkrijging via winkels of loot ontbreekt nog; alleen de geïsoleerde QA geeft ze als testitems.

De fictieve QL30-carbine, het fictieve implant en de gratis recovery uit de eerste versie zijn verwijderd. Multiplayer, de overige professions, volledige inventory/implant- en item-effectsystemen, economie, raids en andere AO-content blijven onderdeel van het einddoel.

## Data, bronnen en opslag

De client-decoder leest nu 120.569 item-stattabellen. Een afzonderlijke vergelijking bevestigt 66 velden van de zeven bestaande itemdefinities. Niet-ondersteunde recorddelen blijven expliciet geregistreerd; volledige functie-uitvoering en een import van alle items zijn nog niet gereed. Zie `Tools/decode_client_items.py`, `Tools/compare_client_items.py` en `Artifacts/client-item-comparison.json`.

De officiële client **18.8.50_EP1** is lokaal als referentie uitgepakt. `Tools/audit_client_reference.py` controleert de database; alle 459.308 resource-records hebben overeenkomende headers en leesbare payloads. Dit is nog geen import van volledige itemgegevens of bewijs van de huidige liveversie. Herkomst, checksums, extractie en beperkingen staan in `Docs/CLIENT-REFERENCE.md` en `Artifacts/client-reference-audit.json`.

`Unity/Assets/Resources/AO` bevat de getransformeerde referentietabellen en `provenance.json` met broncommits en hashes. De itemindex is **18.08.58.01**; afzonderlijk opgezochte item- en nanorecords komen uit AO Galaxy **18.8.62**. `executable-items.json` bewaart zeven exacte itemdefinities met bronlinks; `RawItems` bevat zes vastgelegde AO Index-bronrecords voor belts, memory en body armor; `Tools/import_utility_items.py` zet de beoordeelde operators en On Wear-modifiers om; onbekende QL's worden geweigerd. Deze verschillende snapshots worden niet voorgesteld als één complete live-database. De versievoorkeur is nog niet bevestigd; voorlopig is live inclusief uitbreidingen het doel.

`Tools/import_ao_reference.py` genereert de tabellen vanuit de vastgelegde AOSharp-, CellAO- en Nadybot-checkouts onder `Research`. Bronvermelding en datalicenties staan ook in `Unity/Assets/StreamingAssets/ThirdParty` en gaan mee in de build. Details en links staan in `Docs/FIDELITY.md`.

De character-save gebruikt nu `soldier-v3.json` onder `%USERPROFILE%/AppData/LocalLow/RebornPrototype/Anarchy Reborn - District Prototype`, inclusief iteminstanties en equipment-slots. Bij ontbreken van v3 wordt een geldige v2-save met het oorspronkelijke starterwapen gemigreerd; het v2-bestand blijft intact. De oude `soldier-v1.json` wordt niet als AO-build geïmporteerd. Alleen permanente characterdata wordt opgeslagen; actieve missies en tijdelijke buffs nog niet.

## Blender en buildpipeline

- `Art/Blender/DistrictKit.blend`: bewerkbare broncollecties voor alle 11 modellen.
- `Tools/create_models.py`: reproduceerbare Blender-geometrie en FBX-export. Overschrijft de gegenereerde bronkit; sla handmatige varianten apart op.
- `Tools/Build.ps1`: genereert de Unity-scene en Windows-build en controleert de exitcode. Met `-RebuildModels` wordt ook Blender uitgevoerd. Bewaar handmatig bewerkte scenes onder een andere naam.
- `Unity/Assets/Editor/AoValidation.cs`: referentie- en invarianttests. De oudere checks voor verzonnen items zijn vervangen.
- `Artifacts/ao-reference-validation.txt`, `inventory-validation.txt`, `effects-validation.txt`, `damage-validation.txt`, `initiative-validation.txt`, `oe-validation.txt`, `runtime-validation.txt` en `validation.txt`: testresultaten met hun beperkte dekking.
- `Artifacts/*.png`: echte screenshots van de Windows-player, waaronder skills, itemdatabase en nanobibliotheek.

Start de executable met `-qaCapture` voor de runtimecheck en captures. Deze test gebruikt geïsoleerde fixturewaarden, schrijft geen character-save en sluit zichzelf af. Geslaagde checks bewijzen de geteste data en lokale codepaden; ze bewijzen **niet** dat de volledige game 1-op-1 is.

De decoder ondersteunt inmiddels ook tekstargumenten, hash-verwijzingen en shop-records: 120.549 records zijn structureel gelezen; twintig afwijkende records blijven expliciet geregistreerd. Negen decodercontroles en de 66 veldvergelijkingen slagen. Deze dekking betreft het bestandsformaat, niet de uitvoering van alle items in Unity.

Ook de 10.815 nano-resource-records zijn nu structureel gelezen. De twee werkende buffs halen hun gecontroleerde castkosten, casttijd, recharge en eisen uit `self-effects.json`, met clientversie en payloadhash. Het volledige nano-aanbod en alle bijzondere effecten zijn nog niet uitvoerbaar.

Assault Rifle Proficiency is toegevoegd als derde self-buff, met expliciete clientbron 18.8.50_EP1. In de nanobibliotheek kun je ondersteunde programma’s op jezelf casten. Expertise vervangt Proficiency binnen dezelfde nanoline; een zwakkere buff overschrijft de sterkere niet. Het NCU-gebruik telt de vervangen buff niet dubbel. Nano’s leren/verkrijgen en bijzondere stackingregels zijn nog niet voltooid.

De twintig decoderuitzonderingen zijn opgelost: alle 120.569 item- en 10.815 nano-records worden nu structureel gelezen. Veertien decodercontroles slagen. Dit betekent dat de recordgrenzen en argumentformaten kloppen voor deze snapshot; het betekent nog geen volledige uitvoering van alle gamefuncties.

Er zijn nu 97 algemene Proficiency/Expertise-buffs uitvoerbaar via de nanobibliotheek, naast de gedeeltelijke TMS-implementatie. Elke buff gebruikt zijn eigen eisen, timing, NCU, modifier en stackingprioriteit uit client 18.8.50_EP1. Elf varianten blijven expliciet open. Actieve buffs hebben een scrollbare lijst. Nano’s leren/verkrijgen en casts op anderen zijn nog niet geïmplementeerd.

Nano’s moeten nu eerst geleerd zijn. Onder Equipment kun je een crystal uploaden als je aan de eisen voldoet; alleen bij succes wordt de crystal verbruikt. Geleerde programma’s worden opgeslagen. Er zijn 98 gewone crystals gedefinieerd. Winkels, drops en de juiste startercrystals ontbreken nog, dus nieuwe personages en oude prototypesaves krijgen deze programma’s nog niet automatisch. QA gebruikt afzonderlijke testinventories.

Body Boost en de Soldier-startup-crystal zijn nu geïmplementeerd: +20 maximale HP, 1 NCU en 11 nano per cast. De actuele verkrijging via de Arete-container is nog niet aangesloten; alleen QA krijgt de crystal als testfixture. De catalogus bevat nu 99 nano-definities en 106 itemdefinities.

Controleer na een capture-run ook de beelden met Tools/Verify-RuntimeCaptures.ps1. De gameplaychecks detecteren geen zwart renderbeeld; verborgen vensters leverden eerder zwarte screenshots op. De laatste zichtbare run slaagt voor 96 runtimechecks en 19 niet-lege screenshots.


De Unity-broncode bevat nu skill-lockbeheer dat overeenkomt met 44 fixtures uit de oorspronkelijke clientinstructies. Burst controleert lockaanwezigheid; de HUD gebruikt geen vaste negen seconden meer. De daadwerkelijke Burst-aanval, invoer van locks en automatische afteltiming zijn nog niet aangesloten. Deze bronwijziging is gecompileerd en getest, maar nog niet in de eerder geopende executable gebouwd. Na vernieuwen van het oorspronkelijke emulatierapport genereert `Tools/export_skill_lock_fixtures.py` de Unity-fixtures opnieuw.


Er is nu een **losse .NET-server** met een Unity-clientverbinding voor skills en IP. Start `dotnet run --project Server/Anarchy.Server.csproj --no-launch-profile` en vervolgens `Builds/ServerPreview/AnarchyReborn.exe -server http://127.0.0.1:5077`. Deze preview gebruikt nieuwe server-gastsessies; accountlogin en opnieuw verbinden met een bestaand personage ontbreken nog. De server berekent skillkosten, controleert IP/caps en slaat voortgang op. Vervalste velden, ongeldige sessies en herhaalde of gelijktijdige revisies worden geweigerd. Zestien echte client/server-controles slagen. Beweging, combat en inventory zijn nog geen server-authoritative multiplayer. Architectuur, beveiligingsgrenzen en vervolgstappen staan in `Docs/CLIENT-SERVER.md`.










