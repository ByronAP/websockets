using System;
using System.Threading.Tasks;

namespace WebSocketUsageDemo
{
    class Program
    {
        const string WebSocketHost = "ws://websockets.chilkat.io/wsChilkatEcho.ashx";
        const string TestMessage = "Hi, this is a websocket text message.";

        static async Task Main()
        {
            Console.CursorVisible = false;
            Console.WriteLine($"App starting.{Environment.NewLine}");

            // Create an instance of the WebSocketOptions class
            var options = new ByronAP.Net.WebSockets.WebSocketOptions(WebSocketHost);
            // setup options as needed

            // create a client instance and make sure it is automatically disposed of by a using statement
            using var client = new ByronAP.Net.WebSockets.WebSocketClient(options);

            // hookup to the events we want to receive
            client.ConnectionStateChanged += Client_ConnectionStateChanged;
            client.MessageReceived += Client_MessageReceived;
            client.DataReceived += Client_DataReceived;

            // start the connection and ensure it connected sucessfully
            var connResult = await client.ConnectAsync();
            if(!connResult.Item1)
            {
                // connection failed
                Console.WriteLine($"{DateTime.Now} ERROR: {connResult.Item2}");
                await Task.Delay(2000);
                return;
            }

            // send our test message
            Console.WriteLine($"{DateTime.Now} Message Sent: {TestMessage}");
            await client.SendTextAsync(TestMessage);

            // wait a bit before exiting the app
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}App exiting in 10 seconds.");
            // move the cursor back up so we display our echo response in the proper area
            Console.CursorTop = Console.CursorTop-3;
            await Task.Delay(10000);
        }

        /// <summary>
        /// This gets called when the state of the connection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newWebSocketState"></param>
        /// <param name="oldWebSocketState"></param>
        private static void Client_ConnectionStateChanged(object sender, System.Net.WebSockets.WebSocketState newWebSocketState, System.Net.WebSockets.WebSocketState oldWebSocketState)
        {
            Console.WriteLine($"{DateTime.Now} State Changed: from {oldWebSocketState} to {newWebSocketState}");
        }

        /// <summary>
        /// This gets called when a text message is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private static void Client_MessageReceived(object sender, string message)
        {
            Console.WriteLine($"{DateTime.Now} Message Received: {message}");
        }

        /// <summary>
        /// This gets called when binary data is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private static void Client_DataReceived(object sender, byte[] data)
        {
            Console.WriteLine($"{DateTime.Now} Data Received: {data.Length} bytes");
        }
    }
}
