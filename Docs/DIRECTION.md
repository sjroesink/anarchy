> Historisch ontwerp van het eerste prototype. Sinds de aangescherpte 1-op-1-eis is Docs/FIDELITY.md leidend; onderstaande scope en voorbeeldmechanieken zijn niet langer acceptatiecriteria.

# Ontwerprichting en vervolg

## Vertaling van de referentie

De bovenste stadsafbeelding stuurt de compositie: menselijke schaal op de voorgrond, een lange zichtas, hoge verticale massa's, een verre bleke maan, koude diepte en warme kleine accenten. Het district gebruikt een centrale promenade tussen twee waterstroken, industriÃ«le torens met ribben en technische dakdetails, cyaan wayfinding, verlichte ramen en een shuttle boven de stad.

Dit is een eerste geometrische artstudie. De conceptafbeelding is veel gedetailleerder: de huidige versie mist onder meer verweerde oppervlakken, vegetatie, complexe silhouettes, menselijke animatie en de dichtheid van een bevolkte stad. Die verschillen zijn een werkvoorraad, geen bereikte kwaliteit.

## Mechanieken uit het meegeleverde rapport

| Rapportprincipe | In deze iteratie | Volgende verdieping |
|---|---|---|
| Character engineering | IP, skillkosten, implant, buff, NCU, requirement en OE | Data-assets voor items/nanos/professions; echte stat dependency graph |
| Target-based combat | Tab-target, bereik, zichtlijn, cycles, specials, Agg/Def | Attack/recharge apart, threat, resist, damage types, initiative |
| Terminalmissies | Seed + threat â†’ vijanden, beloning, Ã©Ã©n claim | Seeded room graph, objective templates en persistente instances |
| Professions blijven asymmetrisch | Soldier als eerste testpersoon | Doctor en Engineer; healing en pet-control als onderscheidende rollen |
| Sociale wereld | Visuele neutrale stad | Teams, chat, organizations en factiestatus |
| Server-authoritative architectuur | Nog geen netwerk; lokale simulation | Zuivere C# regels losmaken van MonoBehaviours, dedicated zone host en gevalideerde commands |

De exacte getallen in het prototype zijn eigen ontwerpkeuzes. Het rapport motiveert de systemen; het levert geen complete gezaghebbende formule-/contentdatabase. De plus-5 abilitybijdrage is nu constant; een volledige ability/trickle-down-berekening is nog niet aanwezig.

## Aanbevolen volgende bouwstappen

1. **Combat en beweging:** rig en locomotion voor het Blender-personage, projectielen/VFX, audio, hitreacties en leesbare enemy attacks. Test de fun van Ã©Ã©n encounter voordat meer content wordt gemaakt.
2. **Data en regels:** ScriptableObject-definities voor profession, item, nano en implant; pure simulation voor statberekening, damage en mission state. Save-schema versioneren en migraties toevoegen.
3. **Echte missie-instance:** Blender-roomkit, seeded verbindingen, NavMesh, drie objectives, loot en een extraction/return-loop.
4. **Multiplayerproef:** twee spelers op een dedicated zone-server, server-gevalideerde equipment/buff/attack commands en persistente rewards. Daarna pas teams en een derde profession.
5. **Visuele verdieping:** URP/HDRP-keuze op basis van doelhardware, getextureerde facade-kit, LOD's, occlusion, licht/VFX-budgetten en omzetting van HUD naar productinterface.

Een netwerklaag mag de huidige lokale opslag of lokale rewardfunctie niet zonder meer als autoriteit gebruiken. De eerstvolgende netwerk-mijlpaal moet aantonen dat een client geen IP, items of credits kan toekennen en een reward niet dubbel kan claimen.
