using System.Net.WebSockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace incoming_call_recording.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// Accept WebSocket Connection, and then Loop in receiving data transmitted from client.
        /// </summary>
        /// <param name="webSocket"></param>
        public static async Task ProcessRequest(WebSocket webSocket)
        {
            Dictionary<string, FileStream> audioDataFiles = new Dictionary<string, FileStream>();

            try
            {
                string partialData = "";

                while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[2048];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                        try
                        {
                            if (receiveResult.EndOfMessage)
                            {
                                data = partialData + data;
                                partialData = "";

                                if (data != null)
                                {
                                    AudioDataPackets jsonData = JsonConvert.DeserializeObject<AudioDataPackets>(data);

                                    if (jsonData != null && jsonData.kind == "AudioData")
                                    {
                                        // byte[] byteArray = jsonData?.audioData?.data;
                                        byte[] bytes = System.Convert.FromBase64String(jsonData?.audioData?.data);
                                        // File.WriteAllBytes(fileName, bytes);
                                        // string fileName = string.Format("..//{0}.wav", jsonData?.audioData?.participantRawID).Replace(":", "");
                                        string fileName = string.Format("..//{0}.pcm", jsonData?.audioData?.participantRawID).Replace(":", "");
                                        FileStream audioDataFileStream;

                                        if (audioDataFiles.ContainsKey(fileName))
                                        {
                                            audioDataFiles.TryGetValue(fileName, out audioDataFileStream);
                                        }
                                        else
                                        {
                                            audioDataFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                                            audioDataFiles.Add(fileName, audioDataFileStream);
                                        }
                                        await audioDataFileStream.WriteAsync(bytes, 0, bytes.Length);
                                    }
                                    Console.WriteLine(data);
                                }
                            }
                            else
                            {
                                partialData = partialData + data;
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine($"Exception -> {ex}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception -> {ex}");
            }
            finally
            {
                foreach (KeyValuePair<string, FileStream> file in audioDataFiles)
                {
                    file.Value.Close();
                }
                audioDataFiles.Clear();
            }
        }
    }

}
