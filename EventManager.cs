using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Game4Freak.EventManager
{
    public class EventManager : RocketPlugin<EventManagerConfiguration>
    {
        public static EventManager Instance;
        public const string VERSION = "0.3.0.0";
        private string newVersion = null;
        private bool notifyUpdate = false;
        private int frame = 10;
        private int eventIndex;
        private List<bool> sendNotifications;
        private Event nextEvent;

        private bool countdownUIActive;

        private ExampleEvent example_item;
        private MessageEvent example_message;

        public delegate void onEventTriggeredHandler(Event @event);
        public static event onEventTriggeredHandler onEventTriggered;
        public delegate void onEventNotificationHandler(Event @event, float secondsBeforeEvent);
        public static event onEventNotificationHandler onEventNotification;

        protected override void Load()
        {
            Instance = this;
            Logger.Log("EventManager v" + VERSION);

            WebClient client = new WebClient();
            try
            {
                newVersion = client.DownloadString("http://pastebin.com/raw/GAcbHDDd");
            }
            catch (WebException e)
            {
                Logger.Log("No connection to version-check");
            }
            if (newVersion != null)
            {
                if (compareVersion(newVersion, VERSION))
                {
                    Logger.Log("A new EventManager version (" + newVersion + ") is available !!!");
                    notifyUpdate = true;
                }
            }
            if (Configuration.Instance.resetOnReload)
            {
                Configuration.Instance.lastEventUnixTime = getCurrentTime();
                Configuration.Save();
            }
            eventIndex = 0;
            resetSendNotifications();
            nextEvent = null;

            countdownUIActive = false;

            onEventTriggered += onEventTrigger;

            U.Events.OnPlayerConnected += onPlayerConnection;

            example_item = new ExampleEvent();
            example_item.load();
            example_message = new MessageEvent();
            example_message.load();
        }

        protected override void Unload()
        {
            onEventTriggered -= onEventTrigger;

            U.Events.OnPlayerConnected -= onPlayerConnection;

            example_item.unload();
            example_message.unload();

            if (Configuration.Instance.useUICountdown)
            {
                foreach (var sPlayer in Provider.clients)
                {
                    EffectManager.askEffectClearByID(9750, UnturnedPlayer.FromSteamPlayer(sPlayer).CSteamID);
                }
            }
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList()
                {
                    {"notification_about-to-start_minutes", "[EventManager] A event is about to start in {0} minute(s)!" },
                    {"notification_about-to-start_seconds", "[EventManager] A event is about to start in {0} second(s)!" },
                    {"notification_start", "[EventManager] The event: {0} is starting!" },
                    {"next_event_minutes", "[EventManager] The next event starts in {0} minute(s)!" },
                    {"next_event_seconds", "[EventManager] The next event starts in {0} second(s)!" },
                    {"insufficient_players", "[EventManager] There are not enough players on the server for a event!" },
                    {"no_permission", "You dont have permission to do that!" },
                    {"command_invalid", "Invalid! Try {0} {1}" },
                    {"command_invalid_number", "{0} is not a number!" },
                    {"command_invalid_type", "The type: {0} does not exist" },
                    {"command_invalid_parameters", "Invaid Length! Try {0} {1} {2}" },
                    {"command_no_events", "[EventManager] The server has no events" },
                    {"command_forced_next", "[EventManager] The next event has been forced!" },
                    {"command_reset", "[EventManager] The time to the next event has been reset!" },
                    {"command_event_exists", "The event: {0} already exists!" },
                    {"command_event_not_exists", "The event: {0} does not exist!" },
                    {"command_add", "[EventManager] The event: {0} of type: {1} with priority: {2} was added!" },
                    {"command_remove", "[EventManager] the event: {0} was removed!" },
                    {"exampleevent_invalid_ids", "[{0}] There are no item ids to give away!" },
                    {"exampleevent_give_away", "[{0}] You got a free Item!" },
                    {"exampleevent_on_event_notification", "[{0}] Get yourself ready for a gift!" },
                    {"exmapleevent_insufficient_players", "[{0}] There are not enough players on the server for this event!" },
                    {"countdown_message", "Next Event: {0}" },
                    {"countdown_start", "Event Started!" }
                };
            }
        }

        private void Update()
        {
            frame++;
            if (frame % 10 != 0) return;

            if (Configuration.Instance.events.Count == 0)
            {
                Configuration.Instance.lastEventUnixTime = getCurrentTime();
                Configuration.Save();
                return;
            }
            // change
            if (!serverHasMinClients())
            {
                Configuration.Instance.lastEventUnixTime = getCurrentTime();
                Configuration.Save();
                return;
            }

            if (Configuration.Instance.useUICountdown)
            {
                if (!countdownUIActive && (int)(EventManager.Instance.Configuration.Instance.minutesBetweenEvents * 60 - (getCurrentTime() - Configuration.Instance.lastEventUnixTime)) <= Configuration.Instance.countdownStartMin * 60)
                    countdownUIActive = true;

                if (countdownUIActive)
                {
                    foreach (var sPlayer in Provider.clients)
                    {
                        EffectManager.sendUIEffect(9750, 0, true, formatCountdown());
                    }
                }
            }

            Int32 currentUnixTimestamp = getCurrentTime();

            if (currentUnixTimestamp - Configuration.Instance.lastEventUnixTime >= Configuration.Instance.minutesBetweenEvents * 60)
            {
                sendNextEvent();
                resetSendNotifications();
                return;
            }

            List<float> sortedNotifications = Configuration.Instance.minutesNotificationBefore.OrderBy(i => i).ToList();
            for (int i = 0; i < sortedNotifications.Count; i++)
            {
                if (Configuration.Instance.minutesBetweenEvents * 60 - (currentUnixTimestamp - Configuration.Instance.lastEventUnixTime) <= sortedNotifications[i] * 60)
                {
                    if (sendNotifications[i])
                        return;
                    if (i == sendNotifications.Count - 1)
                        defineNextEvent();
                    sendNotifications[i] = true;
                    if (sortedNotifications[i] > 3)
                        UnturnedChat.Say(Translate("notification_about-to-start_minutes", sortedNotifications[i]), UnturnedChat.GetColorFromName(Configuration.Instance.messageColor, Color.green));
                    else
                        UnturnedChat.Say(Translate("notification_about-to-start_seconds", sortedNotifications[i] * 60), UnturnedChat.GetColorFromName(Configuration.Instance.messageColor, Color.green));
                    onEventNotification(nextEvent, sortedNotifications[i] * 60);
                    return;
                }
            }
        }

        public void onEventTrigger(Event @event)
        {
            UnturnedChat.Say(Translate("notification_start", @event.name), UnturnedChat.GetColorFromName(Configuration.Instance.messageColor, Color.green));
        }

        public void sendNextEvent()
        {
            defineNextEvent();
            onEventTriggered(nextEvent);
            if (Configuration.Instance.useUICountdown)
            {
                foreach (var sPlayer in Provider.clients)
                {
                    EffectManager.askEffectClearByID(9750, UnturnedPlayer.FromSteamPlayer(sPlayer).CSteamID);
                }
            }
            countdownUIActive = false;
            Configuration.Instance.lastEventUnixTime = getCurrentTime();
            Configuration.Save();
            nextEvent = null;
        }

        public void defineNextEvent()
        {
            if (nextEvent != null)
                return;
            List<Event> events = Configuration.Instance.events.OrderByDescending(i => i.priority).ToList();
            if (!serverHasMinClients())
            {
                UnturnedChat.Say(Translate("insufficient_players"), UnturnedChat.GetColorFromName(Configuration.Instance.messageColor, Color.green));
                return;
            }
            if (Configuration.Instance.useRandomEvents)
            {
                bool triggered = false;
                while (!triggered)
                {
                    Event triggerEvent = events[UnityEngine.Random.Range(0, events.Count)];
                    if (Provider.clients.Count >= getEventTypeByID(triggerEvent.type).minPlayers)
                    {
                        nextEvent = triggerEvent;
                        triggered = true;
                    }
                }
            }
            else
            {
                bool triggered = false;
                int currentEventIndex = eventIndex;
                while (!triggered)
                {
                    Event triggerEvent = events[currentEventIndex];
                    if (Provider.clients.Count >= getEventTypeByID(triggerEvent.type).minPlayers)
                    {
                        nextEvent = triggerEvent;
                        triggered = true;
                    }
                    else
                    {
                        currentEventIndex++;
                        if (currentEventIndex > events.Count)
                            currentEventIndex = 0;
                    }
                }
                eventIndex++;
                if (eventIndex >= events.Count)
                    eventIndex = 0;
            }
        }

        public static Int32 getCurrentTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public bool serverHasMinClients()
        {
            foreach (var @event in Configuration.Instance.events)
            {
                if (Provider.clients.Count >= getEventTypeByID(@event.type).minPlayers)
                    return true;
            }
            return false;
        }

        private string formatCountdown()
        {
            int timeLeft = (int)(EventManager.Instance.Configuration.Instance.minutesBetweenEvents * 60 - (getCurrentTime() - Configuration.Instance.lastEventUnixTime));
            int[] times = { 0, 0, 0 };
            string countdown = "";
            if (timeLeft <= 1)
            {
                countdown = Translate("countdown_start");
                return countdown;
            }
            while (timeLeft > 0)
            {
                if (timeLeft >= 3600)
                {
                    times[0]++;
                    timeLeft = timeLeft - 3600;
                }
                else if (timeLeft >= 60)
                {
                    times[1]++;
                    timeLeft = timeLeft - 60;
                }
                else
                {
                    times[2] = timeLeft;
                    break;
                }
            }
            for (int x = 0; x < times.Length; x++)
            {
                if (times[x] < 10)
                    countdown = countdown + "0" + times[x];
                else
                    countdown = countdown + times[x];
                if (x < 2)
                    countdown = countdown + ":";
            }
            return Translate("countdown_message", countdown);
        }

        public EventType getEventTypeByID(string id)
        {
            foreach (var type in Configuration.Instance.eventTypes)
            {
                if (type.id.ToLower() == id.ToLower())
                    return type;
            }
            return null;
        }

        public Event getEventByName(string name)
        {
            foreach (var @event in Configuration.Instance.events)
            {
                if (@event.name.ToLower() == name.ToLower())
                    return @event;
            }
            return null;
        }

        public void addEventType(string id, int minParameterAmount, string parameters, int minPlayers)
        {
            if (getEventTypeByID(id) != null)
                return;
            Configuration.Instance.eventTypes.Add(new EventType(id, minParameterAmount, parameters, minPlayers));
            Configuration.Save();
        }

        public void removeEventType(string id)
        {
            if (getEventTypeByID(id) == null)
                return;
            Configuration.Instance.eventTypes.Remove(getEventTypeByID(id));
            Configuration.Save();
        }

        public void resetSendNotifications()
        {
            sendNotifications = new List<bool>();
            foreach (var notification in Configuration.Instance.minutesNotificationBefore)
                sendNotifications.Add(false);
        }

        private bool compareVersion(string version1, string version2)
        {
            return int.Parse(version1.Replace(".", "")) > int.Parse(version2.Replace(".", ""));
        }

        private void onPlayerConnection(UnturnedPlayer player)
        {
            if (player.HasPermission("eventmanager.admin") && notifyUpdate)
            {
                UnturnedChat.Say(player, "A new EventManager version (" + newVersion + ") is available !!! Yours is: " + VERSION, Color.red);
                notifyUpdate = false;
            }
        }
    }
}