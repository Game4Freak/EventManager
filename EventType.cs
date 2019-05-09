using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Game4Freak.EventManager
{
    public class EventType
    {
        [XmlAttribute("id")]
        public string id;
        [XmlElement("minParameterAmount")]
        public int minParameterAmount;
        [XmlElement("parameters")]
        public string parameters;
        [XmlElement("minPlayers")]
        public int minPlayers;

        public EventType()
        {
        }

        public EventType(string id, int minParameterAmount, string parameters, int minPlayers)
        {
            this.id = id;
            this.minParameterAmount = minParameterAmount;
            this.parameters = parameters;
            this.minPlayers = minPlayers;
        }
    }
}
