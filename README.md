# Develope/Debug (Visual Studio)
Use product [documentation](https://docs.countersoft.com/developing-custom-apps/) from gemini.

# Apps
## Event
* [Replace Paragraph](../#replace-paragraph)
* [Search Fremd ID](../wiki/Dokumentation#search-fremd-id)
* [User Domain](../wiki/Dokumentation#user-domain)
## Timer
* [Reset Workspace Badges](../wiki/Dokumentation#reset-workspace-badges)
* [Unlock User](../wiki/Dokumentation#unlock-user)
* [Merge User](../wiki/Dokumentation#merge-user)

## Replace Paragraph
Diese App wurde als Event App programmiert. Es soll dazu dienen die einkommenden E-Mail-Kommentare und E-Mail Tasks neu zu formatieren. Bis jetzt kommen die Kommentare und Taskbeschreibungen unformatiert in das Gemini System. Dadurch haben die Texte überflüssige Zeilenabstände und Tabellen, was der Übersicht schadet.
Mit einem «EventListener» sollten diese überflüssigen Abstände entfernt werden, nachdem ein Kommentar oder ein Task per E-Mail erfasst wurde. Damit erscheinen die Texte von der Beschreibung und vom Kommentar im Gemini viel sauberer und übersichtlicher.
### Anforderungen
* Einkommende E-Mail-Kommentare werden formatiert → Originator Typ: Breeze
* Die Beschreibung der einkommenden E-Mail Tasks müssen formatiert werden → Originator Typ: Breeze
* Ein «IssueBeforeListener» Interface wird benötigt um «Before»-Ereignisse erkennen zu können
* Mit einer Regex werden die überflüssigen Paragraphen und Tabellen herausgefiltert
* Alle überflüssigen Paragraphen werden mit einem «string.Empty» ersetzt
* Alle überflüssigen Tabellen werden ebenfalls mit «string.Empty» entfernt

## Search Fremd ID
Bei den Arbeiten im Support kommt es immer wieder vor, dass Probleme an weitere Anbieter zur Bearbeitung weitergegeben werden. Diese Anbieter verwenden ihre eigenen Tickettools und vergeben dem Task eine eigene ID, welche in Gemini zur Nachverfolgung notiert wird. Mit der neuen Gemini-Version steht ein benutzerdefiniertes Feld «Fremd-ID» zur Verfügung, in das die ID der externen Tools eingefügt werden soll. Die «Search Fremd ID» App erkennt Fremd-IDs automatisch in den Taskbeschreibungen oder Kommentaren. Die gefundene ID wird ins Feld «Fremd-ID» geschrieben. Die einheitliche Erfassung der Fremd-IDs soll der verbesserten Übersichtlichkeit der Tasks dienen.
### Anforderungen
* Die Fremd-IDs aus den Systemen sollen erkannt werden und ins Feld «Fremd-ID» geschrieben werden. Die FeldId wie auch das Search Patterns sind in einer Config konfigurierbar
* Sie werden beim Aktualisieren des Tasks aus der Description oder den Comments aufgerufen
* Ist eine Fremd-Id gesetzt darf sie nicht überschrieben werden
* Bei Änderungen Auditlog nachführen
* Aktualisierungen oder Fehler sollen im System Log ersichtlich sein

## Reset Workspace Badges
In Gemini können eigene Arbeitsbereiche (Workspace) definiert werden, die so konfiguriert werden können, dass eine Badge-Nummer angezeigt wird, welche die Anzahl Änderungen bei Tasks in diesem Arbeitsbereich angibt. Leider wird diese Anzahl nicht aktualisiert, wenn die Tasks bearbeitet wurden. Die App «ResetWorkspaceBadges» soll bei Arbeitsbereichen, in denen keine Tasks mehr zur Bearbeitung vorliegen, die Badge-Nummer wieder auf null setzen. Die Badge-Nummer wird ebenfalls aktualisiert, wenn noch Task vorhanden sind zum Bearbeiten. Damit wird die Übersichtlichkeit der zu erledigenden Aufträge im Supportdienst gewährleistet.
### Anforderungen
* Die App soll automatisch periodisch ausgeführt werden (Intervall konfigurierbar)
* Alle Workspaces wo die cardurl = items ist und der Badge Count > 0 ist, wird geprüft. Wenn beim Aufrufen des Workspace kein Item mehr vorhanden ist, muss der Badge Count überprüft werden.
* Der Badge Count wird passend zu der Anzahl, der noch zu erledigen Tasks, aktualisiert

## User Domain
Tasks in Gemini können mittels Breeze-Applikation direkt per E-Mail erstellt werden. Aus der Maildomäne des Taskerstellers ist ersichtlich, welcher Schule der Task zugeordnet wird. Aus diesem Grund wurde ein kundenspezifisches Feld «Ersteller OE» angelegt, in das die Benutzerdomäne aus der E-Mail-Adresse eingefüllt werden soll. Die automatische Erkennung von Benutzerdomänen ermöglicht eine verbesserte Übersicht über die Tasks, da sie damit nach Schulen geordnet und gefiltert werden können. Zusätzlich zur automatischen Erkennung der Benutzerdomäne soll in einem zweiten Schritt die in Gemini hinterlegten Benutzer dieser Domäne dem Task als Beobachter (Watcher) hinzugefügt werden. Hintergrund dieser Massnahme ist, dass die Superuser der Schule als Gemini-Benutzer hinterlegt sind und über alle Aktivitäten in ihrem Zuständigkeitsbereich informiert sein sollen. Da dies nur für Schuldomänen gilt, wird eine Blacklist benötigt, in der die Domains aufgelistet werden, für die keine Watcher hinterlegt werden sollen.
### Anforderungen
* Die E-Mail-Domäne des Taskerstellers («Quelle» oder «Erstellt von») wird erkannt und ins Custom Field «Ersteller OE» geschrieben (AppConfig key=customFieldNameDomain).
* Alle mit dieser Domain erfassten Gemini-User werden als Task Beobachter (Watcher) an den Task hinzugefügt. Ausnahme: Domains auf einer Blacklist (AppConfig key=blacklist). 
* Die App wird beim Erstellen eines Tasks ausgeführt.
* Sicherstellen, dass ein Auditlog (Historie) geschrieben wird

## Unlock User
Ein Benutzer von Gemini wird gesperrt, wenn er sich fünf Mal mit dem falschen Passwort anmeldet. Damit die gesperrten Benutzer nicht manuell entsperrt werden müssen, wurde eine Timer App programmiert, die den Benutzer per E-Mail über den Vorfall informiert und die Sperrung nach 15 Minuten (konfigurierbar) wieder entsperrt. Im Inhalt der E-Mail soll auf den Zeitpunkt der Entsperrung sowie auf die Möglichkeit, das Konto vorher durch einen Gemini-Administrator entsperren zu lassen, hingewiesen werden. Die E-Mail soll je nach Sprache des Benutzers auf Deutsch oder Französisch verschickt werden. Diese App hat den Vorteil, dass das Userkonto auch ausserhalb der regulären Arbeitszeiten entsperrt wird.
### Anforderungen
* Die App soll automatisch periodisch ausgeführt werden (Intervall konfigurierbar)
* Die gesperrten Benutzer erhalten eine E-Mail in ihrer Nutzersprache mit dem Zeitpunkt der Entsperrung (konfigurierbar sprache bsp. de-CH oder fr-CH).
* Nach 15 Minuten wird das gesperrte Benutzerkonto automatisch entsperrt (konfigurierbar). 
* Sperrung/Entsperrung von Usern wird im Systemlog nachgeführt.
* Die System Interaktionen müssen im Systemlog eingetragen werden, damit Auswertungen gemacht werden können.

## Merge User
Die Benutzer im Gemini können deaktiviert werden. Die deaktivierten Benutzer im Gemini System müssen nach einiger Zeit entfernt werden. Um die Benutzer entfernen zu können, müssen die Benutzer gemerged werden auf einen anderen Benutzer. Dadurch wird der deaktivierte Benutzer von allen Task befreit und kann entfernt werden.
Mit einer Timer App werden regelmässig nach deaktivierten Benutzer gesucht, die in einem bestimmten Zeitraum befinden. Sobald ein Benutzer gefunden wurde, wird diese automatisch an eine Person aus der gleichen Organisation gemerged. Danach wird der Benutzer endgültig aus dem System gelöscht. Damit gibt es keine überflüssigen Benutzer mehr die für immer deaktiviert bleiben. 
### Anforderungen
* Die deaktivierten Benutzer werden gemerged, die in einem bestimmten Zeitraum sind.
* Die Zeit (Tage) ist konfigurierbar.
* Der deaktivierte Benutzer wird an einen aktivierten Benutzer aus der gleichen Organisation (Maildomäne) gemerged. Dabei wird der erste aktive Benutzer gesucht sortiert nach Loginname (alphabetisch aufsteigend).
* Die erfolgreich gemerged Benutzer werden aus dem System entfernt.
* Die App läuft periodisch (konfigurierbar). 
* Jede Systeminteraktion (Mergevorgang und Löschvorgang) wird in das Systemlog geschrieben.
* Errors sind im Systemlog ersichtlich.
