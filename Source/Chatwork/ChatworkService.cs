using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Newtonsoft.Json;

namespace ChatworkJenkinsBot
{
    public sealed class ChatworkService : Singleton<ChatworkService>
    {
        //----- params -----
        
        
        //----- field -----
        
        private ChatworkClient client = null;

        private ChatworkConfig config = null;

        private DateTime nextFetchTime = default;

        //----- property -----


        //----- method -----

        private ChatworkService(){ }

        public async Task Initialize()
        {
            Console.WriteLine("Initialize ChatWorkService");

            config = new ChatworkConfig();

            await config.Load();

            client = new ChatworkClient(config.RoomId, config.ApiKey);

            nextFetchTime = DateTime.Now;
        }

        public async Task Fetch()
        {
            if (DateTime.Now < nextFetchTime){ return; }

            var json = await client.GetMessage(true);
            
            ParseMessages(json);
            
            Console.WriteLine(json);

            nextFetchTime = DateTime.Now.AddSeconds(30);
        }

        private void ParseMessages(string json)
        {
            // [To:1976595]サポートロボ(NN)さん\nbuild ios feature/mpc

            var messages = JsonConvert.DeserializeObject<ChatworkClient.MessageData[]>(json);
        }
    }
}
