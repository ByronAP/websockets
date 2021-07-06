namespace ByronAP.Net.WebSockets
{
    public class Message
    {
        public MessageType MessageType { get; set; }
        public byte[] Data { get; set; }
    }
}
