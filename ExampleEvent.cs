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
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Game4Freak.EventManager
{
    public class ExampleEvent
    {
        // Setting variables for this EventType
        private static EventType type = new EventType("freeRandomItem", 2, "<minplayers> <itemIDs[]>", 2);

        public ExampleEvent()
        {
        }

        public void load()
        {
            type.minPlayers = EventManager.Instance.Configuration.Instance.freeRandomItemEventMinPlayers;
            // Adding EventType to EventManager
            EventManager.Instance.removeEventType(type.id);
            EventManager.Instance.addEventType(type.id, type.minParameterAmount, type.parameters, type.minPlayers);
            // Connecting to EventManager events
            EventManager.onEventTriggered += onEventTrigger;
            EventManager.onEventNotification += onEventNotify;
        }

        public void unload()
        {
            // Disconnecting from EventManager events
            EventManager.onEventTriggered -= onEventTrigger;
            EventManager.onEventNotification -= onEventNotify;
        }

        // EventManager.onEventNotification
        private void onEventNotify(Event @event, float secondsBeforeEvent)
        {
            if (@event.type == type.id && EventManager.Instance.Configuration.Instance.freeRandomItemEventPreNotification)
            {
                List<float> sortedNotifications = EventManager.Instance.Configuration.Instance.minutesNotificationBefore.OrderBy(i => i).ToList();
                if (secondsBeforeEvent == sortedNotifications[0] * 60)
                {
                    UnturnedChat.Say(EventManager.Instance.Translate("exampleevent_on_event_notification", @event.name), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                }
            }
        }

        // EventManager.onEventTriggered
        private void onEventTrigger(Event @event)
        {
            if (@event.type == type.id)
            {
                int minPlayers;
                if (int.TryParse(@event.parameters[0], out minPlayers) && Provider.clients.Count < minPlayers)
                {
                    UnturnedChat.Say(EventManager.Instance.Translate("exmapleevent_insufficient_players", @event.name), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                    return;
                }
                List<string> parameters = @event.parameters;
                parameters.RemoveRange(0, 1);
                if (!parametersHaveItemID(parameters))
                {
                    UnturnedChat.Say(EventManager.Instance.Translate("exampleevent_invalid_ids", @event.name), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                    return;
                }
                foreach (var sPlayer in Provider.clients)
                {
                    UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(sPlayer);
                    bool fin = false;
                    while (!fin)
                    {
                        ushort itemID;
                        if (ushort.TryParse(parameters[EventManager.Instance.randomNum(0, parameters.Count - 1)], out itemID))
                        {
                            player.GiveItem(itemID, 1);
                            UnturnedChat.Say(player, EventManager.Instance.Translate("exampleevent_give_away", @event.name), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                            fin = true;
                        }
                    }
                }
            }
        }

        public bool parametersHaveItemID(List<string> parameters)
        {
            ushort itemID;
            foreach (var parameter in parameters)
            {
                if (ushort.TryParse(parameter, out itemID))
                    return true;
            }
            return false;
        }
    }
}
