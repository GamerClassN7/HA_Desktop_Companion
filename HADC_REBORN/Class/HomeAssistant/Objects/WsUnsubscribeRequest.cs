namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    internal class WsUnsubscribeRequest
    {
        public int id = 1;
        public string type = "unsubscribe_events";
        public int subscription;
    }
}