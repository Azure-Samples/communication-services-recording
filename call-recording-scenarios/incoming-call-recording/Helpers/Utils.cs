using incoming_call_recording.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            WebSocketReceiveResult? receiveResult = null;
            var activeCall = new ActiveCall
            {
                Stream = new MemoryStream()
            };
            try
            {
                string partialData = "";

                while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
                {
                    byte[] receiveBuffer = new byte[2048];
                    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

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
                                    //AudioDataPackets jsonData = JsonConvert.DeserializeObject<AudioDataPackets>(data);

                                    var jsonData = JsonSerializer.Deserialize<AudioDataPackets>(data,
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                    if (jsonData != null && jsonData.kind == "AudioMetadata")
                                    {
                                        Console.WriteLine($"Audio Metadata: {JsonSerializer.Serialize(jsonData.AudioMetadata)}");
                                        if (CallContextService.MediaSubscriptionIdsToServerCallId.TryGetValue(jsonData.AudioMetadata?.SubscriptionId, out var serverCallId))
                                        {
                                            if (CallContextService.GetActiveCall(serverCallId)?.Stream != null)
                                            {
                                                Console.WriteLine($"This stream is already being processed.  Ending this websocket connection.");
                                                return;
                                            }
                                            else
                                            {
                                                activeCall.SubscriptionId = jsonData.AudioMetadata?.SubscriptionId;
                                                activeCall = CallContextService.SetActiveCall(serverCallId, activeCall);
                                            }
                                        }
                                    }

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
                                        await activeCall.Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
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

                if (activeCall.StopRecordingTimer?.IsRunning ?? false)
                {
                    // Takes 10 seconds for the Cancellation token to timeout after media stream is stopped
                    var elapsedTime = activeCall.StopRecordingTimer.ElapsedMilliseconds - 10000;
                    activeCall.StopRecordingTimer.Stop();
                    Console.WriteLine($"*******RECORDING STOPPED elapsed milliseconds: {elapsedTime}  *******");
                }
                activeCall.Stream.Close();

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, CancellationToken.None);
            }
        }
    }

}
