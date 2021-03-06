﻿using System;
using System.Net.Http;
using Xamarin.Forms;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace CrunchyrollPlus
{
    public class CrunchyrollApi
    {
        public HttpClient crunchyClient;
        private string sessionId="";
        private const string MEDIAPROPSELECTOR = "media.stream_data,media.playhead, media.name,media.media_id,media.description,media.screenshot_image,media.free_available,premium_available,media.episode_number,media.series_id,media.collection_id";
        public QueueEntry[] queue;
        private CrunchyrollApi()
        {
            crunchyClient = new HttpClient();
            crunchyClient.BaseAddress = new Uri("https://api.crunchyroll.com");
        }
        private static readonly CrunchyrollApi singleton = new CrunchyrollApi();
        public static CrunchyrollApi GetSingleton()
        {
            return singleton;
        }

        #region Session
        public struct SessionResponse
        {
            public bool success;
            public bool authed;
            public SessionResponse(bool success, bool authed)
            {
                this.success = success;
                this.authed=authed;
            }
        }  


        /// <summary>
        /// Starts a session with the crunchyroll api
        /// </summary>
        /// <returns>If request is successful </returns>
        public Task<SessionResponse> StartSession(bool useUs)
        {
            
            return Task.Run(async () => {
                string url = GetSessionUrl(useUs);
                
                HttpResponseMessage res = await crunchyClient.PostAsync(url, null);
                Debug.WriteLine("LOG: URL: " + url);
                if (res.IsSuccessStatusCode)
                {
                    string resData = await res.Content.ReadAsStringAsync();
                    Debug.WriteLine("LOG: SESSIONRESPONSE: " + resData + "    END");
                    JObject o = JObject.Parse(resData);
                    if ((bool)o["error"])
                    {
                        return new SessionResponse(false, false);
                    }
                    else
                    {

                        JObject data = (JObject)o["data"];
                        sessionId = (string)data["session_id"];
                        Debug.WriteLine("LOG: AUTH"+data["auth"].Type);
                        if (data["auth"].Type == JTokenType.Null)
                        {
                            Debug.WriteLine("LOG: auth null");
                            return new SessionResponse(true, false);
                        }
                        else
                        {
                            return new SessionResponse(true, true);

                        }
                    }
                }
                return new SessionResponse(false, false);


            });
            
        }
        #endregion
        #region Login
        public struct LoginResponse
        {
            public bool success;
            public string message;
            public LoginResponse(bool success, string message)
            {
                this.success = success;
                this.message = message;
            }
        }
        public Task<LoginResponse> Login(string username, string password, bool staySignedIn)
        {
            return Task.Run(async () => {

                HttpResponseMessage httpResponseMessage = await crunchyClient.PostAsync(GetPath("login", $"&account={username}&password={password}"), null);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string res = await httpResponseMessage.Content.ReadAsStringAsync();
                    JObject o = JObject.Parse(res);
                    if ((bool)o["error"])
                    {
                        return new LoginResponse(false, (string)o["message"]);
                    }
                    else
                    {
                        JObject data = (JObject)o["data"];
                        JObject user = (JObject)data["user"];
                        Application.Current.Properties["userId"] = (string)user["user_id"];
                        if (staySignedIn)
                        {
                            Application.Current.Properties["auth"] = (string)data["auth"];
                            await Application.Current.SavePropertiesAsync(); //Important if you want to save the data
                        }
                        
                        return new LoginResponse(true, "");

                    }
                }
                return new LoginResponse(true, "HE");
            });
        }
        #endregion
        #region Queue
        public struct QueueEntry
        {
            public Media mostLikely;
            public Series series;

            /// <summary>
            /// Successful request
            /// </summary>
            /// <param name="success"></param>
            /// <param name="playhead"></param>
            /// <param name="mostLikely"></param>
            /// <param name="series"></param>
            public QueueEntry( Media mostLikely, Series series)
            {
                this.mostLikely = mostLikely;
                this.series = series;
            }
            
        }
        public struct GetQueueResponse
        {
            public QueueEntry[] entry;
            public bool success;
            public string message;

            /// <summary>
            /// Unsucceful
            /// </summary>
            /// <param name="success"></param>
            /// <param name="message"></param>
            public GetQueueResponse( bool success, string message)
            {
                entry = new QueueEntry[0];
                this.success = success;
                this.message = message;
            }
            public GetQueueResponse(QueueEntry[] queueEntries)
            {
                success = true;
                message = "";
                this.entry = queueEntries;
            }
        }
        public Task<GetQueueResponse> GetQueue()
        {
            return Task.Run(async () =>
            {
                const string FIELDS = "&fields=most_likely_media, series,media.playhead, media.name,media.media_id,media.description,media.screenshot_image,media.free_available,media.premium_available,media.episode_number,media.series_id,media.duration,media.collection_id";
                string url = GetPath("queue", "&media_types=anime"+FIELDS);
                
                HttpResponseMessage res = await crunchyClient.PostAsync(url, null);
                if (res.IsSuccessStatusCode)
                {
                    
                    string jsonString = await res.Content.ReadAsStringAsync();
                    JObject o = JObject.Parse(jsonString);
                    
                    Console.WriteLine("LOG: QUEUE       " + jsonString);
                    if ((bool)o["error"])
                    {
                        return new GetQueueResponse(false, (string)o["message"]);
                    }
                    else
                    {
                        JArray data = (JArray)o["data"];
                        QueueEntry[] a = data.Select(i => {


                            JObject mostLike = (JObject)i["most_likely_media"];
                            string temp = i.ToString();

                            Media mostLikely = new Media(mostLike);
                            JObject s = (JObject)i["series"];
                            Series series = new Series(s);
                            return new QueueEntry(mostLikely, series);
                        }).ToArray();
                        queue = a;
                        return new GetQueueResponse(a);
                    }

                }
                return new GetQueueResponse(false, "Unknown error");
                

            });
        }
        #endregion
        #region StreamData
        public struct StreamDataResponse
        {
            public bool success;
            public string message;
            public string url;
            public int playhead;

            /// <summary>
            /// Unsuccessful response
            /// </summary>
            /// <param name="message"></param>
            public StreamDataResponse(string message)
            {
                this.message = message;
                this.success = false;
                url = "";
                playhead = 0;
            }
            /// <summary>
            /// Successful response
            /// </summary>
            /// <param name="url"></param>
            public StreamDataResponse(string url, int playhead)
            {
                this.url = url;
                success = true;
                message = "";
                this.playhead = playhead;
            }

            
        }

        public Task<StreamDataResponse> GetStreamData(string mediaId)
        {
            return Task.Run(async () =>
            {
                string url = GetPath("info", $"&media_id={mediaId}&fields=media.stream_data,media.playhead");
                HttpResponseMessage res = await crunchyClient.PostAsync(url, null);
                if (res.IsSuccessStatusCode)
                {
                    string jsonString = await res.Content.ReadAsStringAsync();
                    JObject o = (JObject)JObject.Parse(jsonString);
                    if ((bool)o["error"])
                    {
                        return new StreamDataResponse((string)o["message"]);
                    }
                    else
                    {
                        JArray streams = (JArray)o["data"]["stream_data"]["streams"];
                        if (streams.Count == 0)
                        {
                            return new StreamDataResponse("NoStream");
                        }

                        return new StreamDataResponse((string)streams[0]["url"], (int)o["data"]["playhead"]);
                    }
                }

                return new StreamDataResponse("Unknown Error");
            });
        }
        #endregion
        #region GetCollection
        public struct GetCollectionsResponse
        {
           public Collection[] collections;
           public bool success;
           public string message;

            public GetCollectionsResponse(Collection[] collections)
            {
                this.collections = collections;
                success = true;
                message = "";
            }
            public GetCollectionsResponse(string message)
            {
                this.message = message;
                collections = new Collection[0];
                success = false;
            }
        }
        public Task<GetCollectionsResponse> GetCollections(string id)
        {
            return Task.Run(async () =>
            {
                
            HttpResponseMessage res = await crunchyClient.GetAsync(GetPath("list_collections", $"&series_id={id}"));
                Debug.WriteLine("LOG: URL: "+ GetPath("list_collections", $"&series_id={id}"));
                if (res.IsSuccessStatusCode)

                {
                    JObject o = JObject.Parse(await res.Content.ReadAsStringAsync());
                    if ((bool)o["error"])
                    {
                        return new GetCollectionsResponse((string)o["message"]);
                    }
                    else
                    {
                        JArray a = (JArray)o["data"];
                        Collection[] collections = (Collection[])a.Select(i => new Collection((string)i["name"], (string)i["collection_id"])).ToArray();

                        return new GetCollectionsResponse(collections);

                    }

                    
                }
                return new GetCollectionsResponse("Unknown error");

            });
        }
        #endregion
        #region Autocomplete
        public struct AutocompleteResponse
        {
            public Series[] series;
            public bool success;
            public string message;
            public AutocompleteResponse(Series[] series)
            {
                this.series = series;
                success = true;
                message = "";
            }
            public AutocompleteResponse(string message)
            {
                this.message = message;
                success = false;
                series = null;
            }
            

        }
        public Task<AutocompleteResponse> Autocomplete(string query)
        {
            return Task.Run(async () =>
            {
                HttpResponseMessage res = await crunchyClient.PostAsync(GetPath("autocomplete", $"&media_types=anime&q={query}"), null);
                if (res.IsSuccessStatusCode)
                {
                    JObject o = JObject.Parse(await res.Content.ReadAsStringAsync());
                    if ((bool)o["error"])
                    {
                        return new AutocompleteResponse((string)o["message"]);

                    }
                    else
                    {
                        JArray a = (JArray) o["data"];
                        Series[] series = a.Select(i => new Series((JObject)i)).ToArray();
                        return new AutocompleteResponse(series);
                    }
                }
                else
                {
                    return new AutocompleteResponse("Network problem");
                }
                
            });
        }
        #endregion
        #region ListMedias
        public struct ListMediaResponse
        {
            public bool success;
            public string message;
            public Media[] medias;
            public ListMediaResponse(string message)
            {
                success = false;
                this.message = message;
                medias = null;
            }
            public ListMediaResponse(Media[] medias)
            {
                success = true;
                message = "";
                this.medias = medias;
            }
        }   
        public Task<ListMediaResponse> GetMedias(string collectionID)
        {
            return Task.Run(async () => 
            {
                const string FIELDS = "&fields=media, meida.series_id,media.media_id,media.name,media.description,media.screenshot_image,media.free_available,media.premium_available,media.episode_number,media.collection_id,media.duration,media.series_id,media.playhead";
                HttpResponseMessage res = await crunchyClient.PostAsync(GetPath("list_media", $"&limit=1000&collection_id={collectionID}"+FIELDS),null);
                if (res.IsSuccessStatusCode)
                {
                    string d = await res.Content.ReadAsStringAsync();
                    JObject o = JObject.Parse(d);
                    if ((bool)o["error"])
                    {
                        return new ListMediaResponse((string)o["message"]);
                    }
                    else
                    {
                        JArray a = (JArray)o["data"];
                        Media[] medias = a.Select(i => new Media((JObject)i)).ToArray();
                        return new ListMediaResponse(medias);
                    }
                }
                else
                {
                    return new ListMediaResponse("Unknown network error");
                }
                
            });
        }
        #endregion
        #region GetSeries
        public struct GetSeriesResponse
        {
            public bool success;
            public string message;
            public Series series;
            
            public GetSeriesResponse(string message)
            {
                success = false;
                this.message = message;
                series = new Series();
            }

            public GetSeriesResponse(Series series)
            {
                this.series = series;
                success = true;
                message = "";
            }
        }
        public Task<GetSeriesResponse> GetSeries(string id)
        {
            return Task.Run(async () =>
            {
                HttpResponseMessage res = await crunchyClient.PostAsync(GetPath("info", $"&series_id={id}"),null);
                if (res.IsSuccessStatusCode)
                {
                    JObject o = JObject.Parse(await res.Content.ReadAsStringAsync());
                    if ((bool)o["error"])
                    {
                        return new GetSeriesResponse((string)o["message"]);
                    }
                    else
                    {
                        return new GetSeriesResponse(new Series((JObject)o["data"]));
                    }
                }
                else
                {
                    return new GetSeriesResponse("Unknown network error");
                }
                
            });
        }

        #endregion
        #region Log progress

        public Task<bool> LogProgess(string mediaId, int playhead)
        {
            return Task.Run(async () =>
            {
                string url = GetPath("log", $"&event=playback_status&media_id={mediaId}&playhead={playhead.ToString()}");
                HttpResponseMessage res = await crunchyClient.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    string data = await res.Content.ReadAsStringAsync();
                    JObject o = JObject.Parse(data);
                    return !(bool)o["error"];
                    //TODO: Do something if error by reading code don't know what to do tho, if you done anything wrong then that is code fault not user fault, otherwise server
                }
                else
                {
                    return false;
                }
            });
        }

        #endregion
        #region Add to queue
        public Task<bool> AddToQueue(string seriesId)
        {
            return Task.Run(async ()=>{
                HttpResponseMessage res = await crunchyClient.GetAsync(GetPath("add_to_queue", "&series_id=" + seriesId));
                if (res.IsSuccessStatusCode)
                {
                    JObject o = JObject.Parse(await res.Content.ReadAsStringAsync());
                    return !(bool)o["error"];
                }
                return false;
            });
        }
        #endregion
        #region Remove from queue

        public Task<bool> RemoveFromQueue(string seriesId)
        {
            return Task.Run(async () =>
            {
                HttpResponseMessage res = await crunchyClient.GetAsync(GetPath("remove_from_queue", "&series_id=" + seriesId));
                if (res.IsSuccessStatusCode)
                {
                    return !(bool)JObject.Parse(await res.Content.ReadAsStringAsync())["error"];
                }
               

                return false;
            });
        }
        #endregion
        #region Get duration

        public Task<int> GetDuration(string mediaId)
        {
            return Task.Run(async () =>
            {
                HttpResponseMessage res = await crunchyClient.GetAsync(GetPath("info", "&fields=media.duration&media_id=" + mediaId));
                if (res.IsSuccessStatusCode)
                {
                    JObject o = JObject.Parse(await res.Content.ReadAsStringAsync());
                    if ((bool)o["error"])
                    {
                        return -1;
                    }
                    return (int)o["data"]["duration"];
                }
                return -1;
            });
        } 
        #endregion



        #region US Session Redundant

        public Task<SessionResponse> GetUsSessionRedundant(bool trySaved = true,int count=0)
        {
            // Doesn't currently work but could be used later if the US api is down

            return Task.Run(async () =>
            {

                if (trySaved && Application.Current.Properties.ContainsKey("lastWorkingProxy"))
                {
                    try
                    {
                        WebProxy usProxy = new WebProxy($"http://{Application.Current.Properties["lastWorkingProxy"]}:80");

                        HttpClient crunchyProxy = new HttpClient(handler: new HttpClientHandler { Proxy = usProxy });
                        crunchyProxy.BaseAddress = new Uri("https://api.crunchyroll.com");
                        HttpResponseMessage resSes = await crunchyProxy.GetAsync(GetSessionUrl(false));
                        if (resSes.IsSuccessStatusCode)
                        {
                            string resData = await resSes.Content.ReadAsStringAsync();
                            JObject o = JObject.Parse(resData);
                            if ((bool)o["error"])
                            {
                                 return await GetUsSessionRedundant(trySaved: false);
                            }
                            else
                            {
                                // Check country 

                                JObject responseData = (JObject)o["data"];

                                sessionId = (string)responseData["session_id"];
                                if (responseData["auth"].Type == JTokenType.Null)
                                {
                                    return new SessionResponse(true, false);
                                }
                                else
                                {
                                    return new SessionResponse(true, true);

                                }
                            }
                        }
                        else
                        {
                             return await GetUsSessionRedundant(trySaved: false);

                        }

                    }
                    catch (Exception e)
                    {
                        return await GetUsSessionRedundant(trySaved: false);
                    }

                }

                if (count == 5) return new SessionResponse(false, false);

                try
                {
                    HttpClient getProxy = new HttpClient();
                    getProxy.BaseAddress = new Uri("https://api.getproxylist.com/");
                    HttpResponseMessage res = await getProxy.GetAsync("/proxy?country[]=US&anonymity[]=high%20anonymity&port[]=80");

                    if (res.IsSuccessStatusCode)
                    {
                        string data = await res.Content.ReadAsStringAsync();

                        string ip = (string)JObject.Parse(data)["ip"];
                        WebProxy usProxy = new WebProxy($"http://{ip}:80");
                        HttpClient crunchyProxy = new HttpClient(handler: new HttpClientHandler { Proxy = usProxy });
                        crunchyProxy.BaseAddress = new Uri("https://api.crunchyroll.com");

                        HttpResponseMessage resSes = await crunchyProxy.GetAsync(GetSessionUrl(false));

                        if (resSes.IsSuccessStatusCode)
                        {
                            string resData = await res.Content.ReadAsStringAsync();
                            JObject o = JObject.Parse(resData);

                            if ((bool)o["error"])
                            {
                                return await GetUsSessionRedundant(trySaved: false, count = count + 1);
                            }
                            else
                            {
                                // CHeck country
                                

                                JObject responseData = (JObject)o["data"];
                                if (!CheckUsCountryCode(responseData)) return await GetUsSessionRedundant(trySaved: false, count: count + 1);

                                Application.Current.Properties["lastWorkingProxy"] = ip;
                                await Application.Current.SavePropertiesAsync(); //Important if you want to save the data
                                sessionId = (string)responseData["session_id"];

                                if (responseData["auth"].Type == JTokenType.Null)
                                {
                                    Debug.WriteLine("LOG: auth null");
                                    return new SessionResponse(true, false);
                                }
                                else
                                {
                                    return new SessionResponse(true, true);

                                }
                            }
                        }
                        else
                        {
                            return await GetUsSessionRedundant(trySaved: false, count: count + 1);
                        }


                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return await GetUsSessionRedundant(trySaved: false, count: count + 1);
                }

                return new SessionResponse(false, false);


            });

            
        }
        #endregion


        
        #region DownloadVideo           

        private async void DownloadVideo(string mediaId)
        {
            // Future implement use vlc lib to record video then save to file system and then load when needed


            
        }
        #endregion
        private string GetPath(string req, string data)
        {
            return $"/{req}.0.json?session_id={sessionId}{data}";
        }
        private string GetSessionUrl(bool useUs)
        {
            const string deviceType = "com.crunchyroll.windows.desktop";
            string url = $"/start_session.0.json?access_token=LNDJgOit5yaRIWN&device_id={Guid.NewGuid()}&device_type={deviceType}";
            if (useUs) url = "https://api1.cr-unblocker.com/getsession.php?version=1.1";
            if (Application.Current.Properties.ContainsKey("auth"))
            {
                Debug.WriteLine("LOG: ADDED auth");
                string auth = (string)Application.Current.Properties["auth"];
                if (auth != null)
                {
                    url += "&auth=" + auth + "&user_id=" + Application.Current.Properties["userId"].ToString();

                }

            }
            return url;
        }
        private bool CheckUsCountryCode(JObject data)
        {
            return (string)data["country_code"] == "US";
        }



    }
}
