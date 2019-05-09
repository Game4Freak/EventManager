using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Game4Freak.EventManager
{
    public class EventManagerConfiguration : IRocketPluginConfiguration
    {
        public bool freeRandomItemPreNotification;
        public string messageColor;
        public bool useRandomEvents;
        public float minutesBetweenEvents;
        [XmlArrayItem(ElementName = "notification")]
        public List<float> minutesNotificationBefore;

        [XmlArrayItem(ElementName = "event")]
        public List<Event> events;
        [XmlArrayItem(ElementName = "eventType")]
        public List<EventType> eventTypes;

        public void LoadDefaults()
        {
            freeRandomItemPreNotification = true;
            messageColor = "cyan";
            useRandomEvents = true;
            minutesBetweenEvents = 2;
            minutesNotificationBefore = new List<float>() { 1, 0.5f };

            events = new List<Event>();
            eventTypes = new List<EventType>();
        }
    }
}
