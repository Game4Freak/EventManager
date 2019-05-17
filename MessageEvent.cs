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
    public class MessageEvent
    {
        // Setting variables for this EventType
        private static EventType type = new EventType("message", 1, "<message>", 0);

        public MessageEvent()
        {
        }

        public void load()
        {
            // Adding EventType to EventManager
            EventManager.Instance.removeEventType(type.id);
            EventManager.Instance.addEventType(type.id, type.minParameterAmount, type.parameters, type.minPlayers);
            // Connecting to EventManager events
            EventManager.onEventTriggered += onEventTrigger;
        }

        public void unload()
        {
            // Disconnecting from EventManager events
            EventManager.onEventTriggered -= onEventTrigger;
        }

        // EventManager.onEventTriggered
        private void onEventTrigger(Event @event)
        {
            if (@event.type == type.id)
            {
                List<string> parameters = @event.parameters;
                string message = "";
                for (int i = 0; i < parameters.Count; i++)
                {
                    message = message + parameters[i] + " ";
                }
                UnturnedChat.Say(message, UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageEventColor, Color.green));
            }
        }
    }
}
