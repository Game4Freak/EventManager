using Rocket.API;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Game4Freak.EventManager
{
    public class EventManagerConfiguration : IRocketPluginConfiguration
    {
        public bool freeRandomItemEventPreNotification;
        public int freeRandomItemEventMinPlayers;
        public string messageColor;
        public bool useRandomEvents;
        public bool resetOnReload;
        public Int32 lastEventUnixTime;
        public float minutesBetweenEvents;
        [XmlArrayItem(ElementName = "notification")]
        public List<float> minutesNotificationBefore;

        [XmlArrayItem(ElementName = "event")]
        public List<Event> events;
        [XmlArrayItem(ElementName = "eventType")]
        public List<EventType> eventTypes;

        public void LoadDefaults()
        {
            freeRandomItemEventPreNotification = true;
            freeRandomItemEventMinPlayers = 2;
            messageColor = "cyan";
            useRandomEvents = true;
            resetOnReload = true;
            lastEventUnixTime = EventManager.getCurrentTime();
            minutesBetweenEvents = 2;
            minutesNotificationBefore = new List<float>() { 1, 0.5f };

            events = new List<Event>();
            eventTypes = new List<EventType>();
        }
    }
}
