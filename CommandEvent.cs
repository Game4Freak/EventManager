using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Core;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Game4Freak.EventManager
{
    public class CommandEvent : IRocketCommand
    {
        public string Name
        {
            get { return "event"; }
        }
        public string Help
        {
            get { return "Administrate your events"; }
        }

        public AllowedCaller AllowedCaller
        {
            get
            {
                return AllowedCaller.Both;
            }
        }

        public string Syntax
        {
            get { return "<next|forcenext|reset|add|list|remove> <eventname> <eventtype> <priority> <parameters[]>"; }
        }

        public List<string> Aliases
        {
            get { return new List<string> { "events" }; }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() {
                    "eventmanager.admin",
                    "eventmanager.next",
                    "eventmanager.forcenext",
                    "eventmanager.reset",
                    "eventmanager.add",
                    "eventmaanger.list",
                    "evetnmaanger.remove"
                };
            }
        }

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_invalid", "/" + Name, Syntax), Color.red);
                return;
            }
            if (command[0].ToLower() == "next")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[1]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                Int32 currentUnixTimestamp = EventManager.getCurrentTime();
                if ((EventManager.Instance.Configuration.Instance.minutesBetweenEvents * 60 - (currentUnixTimestamp - EventManager.Instance.lastUnixTimestamp)) >= 180)
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("next_event_minutes", (int)(EventManager.Instance.Configuration.Instance.minutesBetweenEvents * 60 - (currentUnixTimestamp - EventManager.Instance.lastUnixTimestamp)) / 60), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                else
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("next_event_seconds", (EventManager.Instance.Configuration.Instance.minutesBetweenEvents * 60 - (currentUnixTimestamp - EventManager.Instance.lastUnixTimestamp))), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                return;
            }
            else if (command[0].ToLower() == "forcenext")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[2]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                if (EventManager.Instance.Configuration.Instance.events.Count == 0)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_no_events"), Color.red);
                }
                if (!EventManager.Instance.serverHasMinClients())
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("insufficient_players"), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                    return;
                }
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_forced_next"), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                EventManager.Instance.defineNextEvent();
                EventManager.Instance.sendNextEvent();
                return;
            }
            else if (command[0].ToLower() == "reset")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[3]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                EventManager.Instance.lastUnixTimestamp = EventManager.getCurrentTime();
                EventManager.Instance.resetSendNotifications();
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_reset"), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                return;
            }
            else if (command[0].ToLower() == "add")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[4]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                if (command.Length < 4)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_invalid_parameters", "/" + Name, "add <eventname> <eventtype> <priority>", ""), Color.red);
                    return;
                }
                if (EventManager.Instance.getEventByName(command[1]) != null)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_event_exists", EventManager.Instance.getEventByName(command[1]).name), Color.red);
                    return;
                }
                int priority;
                if (!int.TryParse(command[3], out priority))
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_no_number", command[3]), Color.red);
                }
                EventType type = EventManager.Instance.getEventTypeByID(command[2]);
                if (type == null)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_invalid_type", command[2]), Color.red);
                    return;
                }
                if (command.Length < type.minParameterAmount + 4)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_invalid_parameters", "/" + Name, "add <eventname> " + type.id + " <priority>", type.parameters), Color.red);
                    return;
                }
                List<string> parameters = command.ToList();
                parameters.RemoveRange(0, 4);
                EventManager.Instance.Configuration.Instance.events.Add(new Event(command[1], type.id, priority, parameters));
                EventManager.Instance.Configuration.Save();
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_add", command[1], command[2], priority), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                return;
            } 
            else if (command[0].ToLower() == "list")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[5]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                UnturnedChat.Say(caller, "[EventManager] Server events:", UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                foreach (var @event in EventManager.Instance.Configuration.Instance.events)
                {
                    string parameters = "";
                    foreach (var parameter in @event.parameters)
                    {
                        parameters = parameters + parameter + ", ";
                    }
                    UnturnedChat.Say(caller, @event.name + ": {Type: " + @event.type + ", Priority: " + @event.priority + ", Parameters: {" + parameters + "}}", UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                }
                return;
            }
            else if (command[0].ToLower() == "remove")
            {
                if (!caller.IsAdmin)
                {
                    if (!((UnturnedPlayer)caller).HasPermission(Permissions[0]) && !((UnturnedPlayer)caller).HasPermission(Permissions[6]))
                    {
                        UnturnedChat.Say(caller, EventManager.Instance.Translate("no_permission"), Color.red);
                        return;
                    }
                }
                Event @event = EventManager.Instance.getEventByName(command[1]);
                if (@event == null)
                {
                    UnturnedChat.Say(caller, EventManager.Instance.Translate("command_event_not_exists", command[1]), Color.red);
                    return;
                }
                EventManager.Instance.Configuration.Instance.events.Remove(@event);
                EventManager.Instance.Configuration.Save();
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_remove", @event.name), UnturnedChat.GetColorFromName(EventManager.Instance.Configuration.Instance.messageColor, Color.green));
                return;
            }
            else
            {
                UnturnedChat.Say(caller, EventManager.Instance.Translate("command_invalid", "/" + Name, Syntax), Color.red);
                return;
            }
        }
    }
}