using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Game4Freak.EventManager
{
    public class Event
    {
        [XmlAttribute("name")]
        public string name;
        [XmlElement("type")]
        public string type;
        [XmlElement("priority")]
        public int priority;
        [XmlArrayItem(ElementName = "parameter")]
        public List<string> parameters;

        public Event()
        {
        }

        public Event(string name, string type, int priority, List<string> parameters)
        {
            this.name = name;
            this.type = type;
            this.priority = priority;
            this.parameters = parameters;
        }
    }
}
