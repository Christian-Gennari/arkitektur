# Presentation: Event-driven Architecture

Det här är ett förslag på en presentation på ungefär 3–5 minuter. Starta projektet med
`dotnet run`, öppna webbadressen som visas i terminalen och ha gärna
`Infrastructure/Events/EventBus.cs` redo i en separat flik.

## Innan presentationen

1. Kontrollera att **Event Monitor** visar `Live`.
2. Kontrollera att todo-listan och statistiken börjar på noll.
3. Använd Development-miljön. Där finns en avsiktlig fördröjning på en sekund som gör
   det asynkrona flödet synligt.
4. Skapa inga test-events precis före presentationen. Eventhistoriken finns bara under
   den aktuella körningen och försvinner när programmet startas om.

## Manus

### 1. Problemet

> I ett vanligt request/response-flöde anropar en komponent nästa komponent direkt och
> väntar på resultatet. Det fungerar bra för enkla operationer, men producenten blir
> kopplad till alla sidoeffekter som loggning, statistik och notifieringar.

> I vår första version hade vi eventnamn och handlers, men EventBus anropade varje
> handler direkt och TodoService väntade tills allt var klart. Det liknade publish/
> subscribe, men demonstrerade inte asynkron eventbearbetning eller eventual consistency.

### 2. Visa arkitekturen

Peka på flödet i **Event Monitor**.

> HTTP-API:et är producenten. Det sparar todo:n och publicerar ett oföränderligt event
> till en Channel. En BackgroundService konsumerar kön och skickar eventet vidare till
> två oberoende consumers: Statistics och Activity log. TodoService känner inte till
> någon av dem.

### 3. Skapa en todo

Skapa todo:n **Förbered EDA-presentation**.

> Todo:n visas direkt eftersom API-requesten redan är klar. Samtidigt står TodoCreated
> som Queued i monitorn och statistiken har ännu inte uppdaterats. Det här är eventual
> consistency: huvudoperationen är färdig före sidoeffekterna.

Vänta tills eventet byter till **Handled**.

> Efter en sekund tar dispatchern eventet. Statistics och Activity log behandlar samma
> event oberoende av varandra. Först därefter ändras statistiken. Fördröjningen är endast
> en inställning för demon; i normal konfiguration är den noll.

### 4. Visa lös koppling och återanvändning

Markera todo:n som klar och radera den.

> Samma pipeline återanvänds för TodoUpdated och TodoDeleted. Vi kan lägga till en ny
> consumer, till exempel notifieringar, utan att ändra TodoService eller de befintliga
> consumers. Varje event har dessutom event-ID, tidpunkt och correlation ID så att vi
> kan följa det genom systemet.

Visa kort `TodoService` och därefter `EventBus`.

> Publish väntar bara på plats i kön. BackgroundService läser kön i FIFO-ordning och
> kör subscribers. Om en consumer misslyckas visas det i monitorn, men den andra får
> fortfarande behandla eventet.

### 5. Avsluta med en ärlig avgränsning

> Det här är en hybridarkitektur: webbläsaren använder fortfarande HTTP request/
> response för commands och queries, medan interna reaktioner är eventdrivna och
> asynkrona. Kön ligger i samma process och är inte beständig. Därför visar lösningen
> EDA-principerna tydligt, men den är inte ett distribuerat produktionssystem.

> Nästa steg vore att ersätta Channel med RabbitMQ eller Kafka och flytta consumers
> till separata processer. Då behövs även retries, dead-letter queue, idempotens och
> beständig lagring.

## Vanliga frågor

**Är det här verkligen EDA utan RabbitMQ?**

Ja. Producenten publicerar events utan att känna till mottagarna, och mottagarna reagerar
asynkront. En extern broker behövs för distribution och bättre hållbarhet, men är inte
ett krav för själva arkitekturstilen. Var tydlig med att detta är lokal EDA.

**Varför är API:et fortfarande REST?**

EDA behöver inte ersätta alla kommunikationssätt. Här passar request/response för
användarens command och query, medan events passar de oberoende sidoeffekterna.

**Vad händer om kön är full?**

Den är begränsad till 100 events. `Publish` väntar då på ledig plats, vilket ger
backpressure i stället för att tappa event tyst.

**Vad händer om en consumer kraschar?**

Felet loggas och visas i Event Monitor. Övriga consumers fortsätter. Den här demon har
inga retries eller dead-letter queue.

**Vad händer om hela applikationen kraschar?**

Events som bara finns i Channel försvinner. En produktionsvariant behöver en beständig
broker och ofta Outbox Pattern för att undvika glappet mellan datalagring och publicering.

**Varför finns en fördröjning på en sekund?**

För att publiken ska hinna se köläget och eventual consistency. Den styrs av
`EventProcessing:DemoDelayMilliseconds`, är `1000` i Development och `0` annars.
