using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;


namespace DiscordBotOnLinux
{
    public class Program
    {
        DiscordSocketClient client; //봇 클라이언트
        CommandService commands;    //명령어 수신 클라이언트
        /// <summary>
        /// 프로그램의 진입점
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new Program().BotMain().GetAwaiter().GetResult();   //봇의 진입점 실행
        }

        /// <summary>
        /// 봇의 진입점, 봇의 거의 모든 작업이 비동기로 작동되기 때문에 비동기 함수로 생성해야 함
        /// </summary>
        /// <returns></returns>
        public async Task BotMain()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {    //디스코드 봇 초기화
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정 
            });
            commands = new CommandService(new CommandServiceConfig()        //명령어 수신 클라이언트 초기화
            {
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정
            });

            //로그 수신 시 로그 출력 함수에서 출력되도록 설정
            client.Log += OnClientLogReceived;
            commands.Log += OnClientLogReceived;
            client.GuildAvailable += Client_GuildAvailable;
            StreamReader streamReader = new StreamReader("DiscordBotKey.txt");
            var key = streamReader.ReadLine();
            await client.LoginAsync(TokenType.Bot, key); //봇의 토큰을 사용해 서버에 로그인
            await client.StartAsync();                         //봇이 이벤트를 수신하기 시작

            client.MessageReceived += OnClientMessage;         //봇이 메시지를 수신할 때 처리하도록 설정

            await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null); //봇에 명령어 모듈 등록

            await Task.Delay(-1);   //봇이 종료되지 않도록 블로킹
        }

        private async Task Client_GuildAvailable(SocketGuild arg)
        {
            var textChannels = arg.TextChannels;
            SocketTextChannel botChannel = null;
            if (textChannels != null)
                botChannel = arg.GetTextChannel(id: textChannels.Where(channel => channel.Name.Contains("봇")).Select(channel => channel.Id).FirstOrDefault());

            if (botChannel != null)
            {
                Console.WriteLine("카드뽑기 봇 연결 알림");
                //await botChannel.SendMessageAsync("카드뽑기 봇 출근했습니다.");
            }
            else
            {
                Console.WriteLine($"{arg.Name} doesn't match the condition" +
                    $"\n 여긴 카드뽑기 봇이 연결되지 않습니다.");
            }
        }

        private async Task OnClientMessage(SocketMessage arg)
        {
            //수신한 메시지가 사용자가 보낸 게 아닐 때 취소
            var message = arg as SocketUserMessage;
            if (message == null) return;

            int pos = 0;

            //메시지 앞에 !이 달려있지 않고, 자신이 호출된게 아니거나 다른 봇이 호출했다면 취소
            if (!(message.HasCharPrefix('!', ref pos) ||
             message.HasMentionPrefix(client.CurrentUser, ref pos)) ||
              message.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, message);                    //수신된 메시지에 대한 컨텍스트 생성   
            //Console.WriteLine($"{message} {pos}");
            var result = await commands.ExecuteAsync(
                            context: context,
                            argPos: pos,
                            services: null);
        }

        /// <summary>
        /// 봇의 로그를 출력하는 함수
        /// </summary>
        /// <param name="msg">봇의 클라이언트에서 수신된 로그</param>
        /// <returns></returns>
        private Task OnClientLogReceived(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());  //로그 출력
            return Task.CompletedTask;
        }
    }

    public class LegendaryPackProbabilityModule : ModuleBase<SocketCommandContext>
    {
        ///<summary>
        ///전카팩 확률
        ///</summary>
        public List<LegendaryCard> cardList = LegendaryCardList.LegendaryCards;
        private Random random = new Random();

        [Command("전카확률")]
        public async Task LegendaryCardPackCommand()
        {
            var listLegendary = cardList;
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < listLegendary.Count; ++index)
            {
                builder.AppendLine($"{listLegendary[index].name} : {string.Format("{0:0.##}", listLegendary[index].probability)}%");
            }

            await Context.Channel.SendMessageAsync(builder.ToString());
        }

        [Command("전카뽑기")]
        public async Task LegendaryCardPick(string command = null)
        {
            if (command == null)
            {
                int cardIndex = random.Next(0, cardList.Count);
                await Context.Channel.SendMessageAsync($"뽑은 카드 {cardList[cardIndex].name}");
            }
            else
            {
                int pickCount;
                if (int.TryParse(command, out pickCount))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("뽑은 카드 목록\n" +
                        "============================\n");
                    pickCount = Math.Min(10, pickCount);
                    if (pickCount == 0)
                        pickCount = 1;
                    for (int count = 0; count < pickCount; ++count)
                    {
                        int cardIndex = random.Next(0, cardList.Count);
                        builder.AppendLine($"{cardList[cardIndex].name}");
                    }

                    await Context.Channel.SendMessageAsync(builder.ToString());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("잘못된 명령어입니다.");
                }
            }
        }

        [Command("My전카뽑기")]
        public async Task LegendaryCardPickDB(string command = null)
        {
            if (command == null)
            {
                int cardIndex = random.Next(0, cardList.Count);
                SaveResultDB(Context.User.Id, cardIndex);
                await Context.Channel.SendMessageAsync($"뽑은 카드 {cardList[cardIndex].name}");
            }
            else
            {
                int pickCount;
                if (int.TryParse(command, out pickCount))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("뽑은 카드 목록\n" +
                        "============================\n");
                    pickCount = Math.Min(10, pickCount);
                    if (pickCount == 0)
                        pickCount = 1;
                    for (int count = 0; count < pickCount; ++count)
                    {
                        int cardIndex = random.Next(0, cardList.Count);
                        builder.AppendLine($"{cardList[cardIndex].name}");
                        SaveResultDB(Context.User.Id, cardIndex);
                    }

                    await Context.Channel.SendMessageAsync(builder.ToString());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("잘못된 명령어입니다.");
                }
            }
        }

        private void SaveResultDB(ulong id, int cardIndex)
        {
            string key = $"{id}_{cardIndex}";
            string userData = RedisDBManager.Instance.GetData(key);
            if (userData == "nil" || userData == null)
            {
                RedisDBManager.Instance.SetData(key, "1");
                Console.WriteLine($"[RedisDB]New Data :{key}, 1");
                return;
            }

            int userCardCount = int.Parse(userData);
            userCardCount++;
            RedisDBManager.Instance.SetData(key, userCardCount.ToString());
        }

        [Command("My컬렉션")]
        public async Task LegendaryCardCollection()
        {
            string totalMessage = "";
            string pointKey = Context.User.Id.ToString();
            totalMessage += "컬렉션 목록입니다.=============\n";
            var cardList = LegendaryCardList.LegendaryCards;
            for (int index = 0; index < cardList.Count; ++index)
            {
                string data = RedisDBManager.Instance.GetData($"{pointKey}_{index}");
                int count = 0;
                if (data != "nil" && data != null)
                {
                    count = int.Parse(data);
                }

                //1 -> 0각
                //2 -> 1각
                //4 -> 2각
                //7 -> 3각
                //11 -> 4각
                //16 -> 5각
                //16+ 나
                int upgradeLevel = 0;
                int remainCount = 0;
                if (count >= 2 && count < 4)
                {
                    upgradeLevel = 1;
                    remainCount = count - 2;
                }
                else if (count >= 4 && count < 7)
                {
                    upgradeLevel = 2;
                    remainCount = count - 4;
                }
                else if (count >= 7 && count < 11)
                {
                    upgradeLevel = 3;
                    remainCount = count - 7;
                }
                else if (count >= 11 && count < 16)
                {
                    upgradeLevel = 4;
                    remainCount = count - 11;
                }
                else if(count >= 16)
                {
                    upgradeLevel = 5;
                    remainCount = count - 16;
                }

                totalMessage += $"{cardList[index].name} : {count}/[TMI]현재 각성 상태 {upgradeLevel}각 +{remainCount}\n";
            }

            await Context.Channel.SendMessageAsync(totalMessage);
        }

        [Command("My컬렉션리셋")]
        public async Task LegendaryCardCollectionReset()
        {
            string totalMessage = "";
            string pointKey = Context.User.Id.ToString();
            totalMessage += "컬렉션 리셋하였습니다.\n";
            var cardList = LegendaryCardList.LegendaryCards;
            for (int index = 0; index < cardList.Count; ++index)
            {
                string data = RedisDBManager.Instance.GetData($"{pointKey}_{index}");
                if (data != "nil")
                {
                    RedisDBManager.Instance.SetData($"{pointKey}_{index}", "0");
                }
            }
        }

        public class PlayAbilityModule : ModuleBase<SocketCommandContext>
        {
            private Random random = new Random();

            [Command("주사위")]
            public async Task BasicDiceCommand(string command = null)
            {
                Console.WriteLine($"command : {command}");
                int maxNumber = 6;
                int diceNumber;
                int.TryParse(command, out maxNumber);
                maxNumber = Math.Max(6, maxNumber);
                diceNumber = random.Next(1, maxNumber + 1);
                Console.WriteLine(maxNumber);
                await Context.Channel.SendMessageAsync($"굴림({1} - {maxNumber}) 결과 : {diceNumber}");
            }
        }

        public class LegendaryCard
        {
            public string name;
            public float probability;

            public LegendaryCard(string name, float probability)
            {
                this.name = name;
                this.probability = probability;
            }
        }

        public static class LegendaryCardList
        {
            public static List<LegendaryCard> LegendaryCards
            {
                get
                {
                    if (m_legendaryCards.Count == 0)
                    {
                        Console.WriteLine("전설카드리스트 Init");
                        StreamReader streamReader = new StreamReader("Legendaries.txt");
                        string prevParsingNames = streamReader.ReadLine();
                        var legendariesNames = prevParsingNames.Split('/');
                        float percent = 100f / legendariesNames.Length;
                        for (int index = 0; index < legendariesNames.Length; ++index)
                        {
                            var legendCard = new LegendaryCard(legendariesNames[index], percent);
                            m_legendaryCards.Add(legendCard);
                            Console.WriteLine($"{legendCard.name} : {percent}%");
                        }
                    }

                    return m_legendaryCards;
                }
            }

            static private List<LegendaryCard> m_legendaryCards = new List<LegendaryCard>();
        }

        public static class LostArkInitializer
        {
            public const string PERCENT_HTML = "https://cdn-lostark.game.onstove.com/uploadfiles/static/798264b5d73248d5ae38f254e6a73afd.html";
        }

        //This is Test Module For RedisDB
        //public class LegendaryCardTestDBModule : ModuleBase<SocketCommandContext>
        //{
        //    [Command("DB뽑기")]
        //    public async Task LegendaryCardPackCommand(string command = null)
        //    {
        //        Console.WriteLine("[DB뽑기]"+command);
        //        Console.WriteLine($"[DB뽑기] UserName [{Context.User.Username}] [UserId {Context.User.Id}]");
        //        //Console.WriteLine(RedisDBManager.Instance.GetData("testInt"));
        //        //Console.WriteLine(RedisDBManager.Instance.GetData("testKey"));

        //        int retryCount = 0;
        //        while (!RedisDBManager.Instance.SetData("Test", "Things"))
        //        {
        //            ++retryCount;
        //            if(retryCount == 10)
        //            {
        //                Console.WriteLine("Retry Failed!");
        //                break;
        //            }
        //        }
        //        //Context.User.Id
        //    }
        //}
    }
}
