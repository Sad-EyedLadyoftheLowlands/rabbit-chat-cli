open System
open System.Threading
open System.Text
open System.Text.Json
open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Data
open FSharp.Configuration

type CreateMessageRequest = {
    SendingUserId: int;
    Content: string;
    RoomId: int;
}

type Message = {
    messageId: int;
    rabbitUserId: int;
    timeSent: DateTime;
    messageContent: string;
    roomId: int;
}

type RabbitUser = {
    rabbitUserId: int;
    username: string;
    password: string;
    token: string;
    refreshToken: string;
    alias: string;
    roomLink: string;
    friends: RabbitUser[];
}

type SimpleSignInRequest = {
    Username: string;
    Password: string;
}

type Settings = YamlConfig<"Config.yaml">
let config = Settings()

let printWelcome () =
    printfn "%s" "********** WELCOME TO RABBIT CHAT **********"

let displayHelp () =
    printfn "%s" "Display help message generated from list of commands"

let setup () = 
    printWelcome()
    printfn "%s" "Setting up application"

let handleInput input = 
    match input with
    | "test" -> printfn "%s" "fuck you"
    | _ -> printfn "%s" "fuck you anyway"

let handleShutdown () = Environment.Exit(0)

let sendMessage (messageRequest : CreateMessageRequest) =
    Http.RequestString(config.Http.ApiURL.ToString() + "message",
        headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ], 
        body = TextRequest (JsonSerializer.Serialize(messageRequest)) )

let getRoomMessages () =
    Http.RequestString(config.Http.ApiURL.ToString() + "message/4")
    |> JsonSerializer.Deserialize<Message[]>

let getFriends () =
    Http.RequestString(config.Http.ApiURL.ToString() + "user/getfriends/1")
    |> JsonSerializer.Deserialize<RabbitUser[]>

let listFriends () = 
    getFriends()
    |> Array.iter (fun friends -> printfn "User: %s(%s)" friends.alias friends.username)

let listTestRoomMessages () =
    getRoomMessages()
    |> Array.take 10
    |> Array.iter (fun message -> printfn "%s" message.messageContent)

let handleCommand input =
    printfn "%s" "processing command"
    match input with
    | "~quit" -> handleShutdown()
    | "~friends" -> listFriends()
    | "~help" -> displayHelp()
    | "~testroom" -> listTestRoomMessages()
    | _ -> printfn "%s" input

let rec readInput () =
    printf "%s" "> "
    let input = Console.ReadLine()
    match input with
    | input when input.StartsWith("~") -> handleCommand input 
    | _ -> handleInput input

    readInput()

let subscribeMq (token : CancellationTokenSource) =
    let hostName = "localhost"
    let exchange = "rabbit-test"
    let routingKey = ""
    let factory = ConnectionFactory(HostName = hostName, UserName = "guest", Password = "guest")
    use connection = factory.CreateConnection()
    use channel = connection.CreateModel()
    channel.ExchangeDeclare(exchange = exchange, ``type`` = ExchangeType.Fanout, durable = false)
    
    let queueName = channel.QueueDeclare().QueueName
    channel.QueueBind(queue = queueName, exchange = exchange, routingKey = routingKey);

    let consumer = EventingBasicConsumer(channel)
    consumer.Received.AddHandler(new EventHandler<BasicDeliverEventArgs>(fun sender (data:BasicDeliverEventArgs) -> 
        let message = Encoding.UTF8.GetString(data.Body.Span)
        printfn "consumed: %A" message))

    let consumeResult = channel.BasicConsume(queue = "", autoAck = true, consumer = consumer)

    while not token.IsCancellationRequested do
        Thread.Sleep(500)

let sendMessageTest () =
    {
        SendingUserId = 1;
        Content = "test from f#";
        RoomId = 4;
    }
    |> sendMessage
    |> printfn "%s"

let changeConfigTest () =
    let config = Settings()
    config.Authentication.Username <- "newusername"
    config.Save("Config.yaml")

let login loginRequest =
    Http.RequestString(config.Http.ApiURL.ToString() + "auth",
        headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ],
        body = TextRequest (JsonSerializer.Serialize(loginRequest)))

let promptUserForLogin () =
    printfn "%s" "No login credentials found!"
    printf "%s" "Please enter your username: > "
    let username = Console.ReadLine()
    printf "%s" "Please enter your password: > "
    let password = Console.ReadLine()
    login {
        Username = username;
        Password = password;
    }
    // |> match res with
    // | :? authUser as RabbitUser -> printfn "%A" authUser
    // | :? res as string -> printfn "%A" res

let isLoggedInGuard () =
    let config = Settings()
    let user = config.Authentication.Username
    match user with
    | "" -> user
    | _ -> login {
        Username = config.Authentication.Username;
        Password = config.Authentication.Password;
        }

[<EntryPoint>]
let main argv =
    let token = new CancellationTokenSource()

    promptUserForLogin()
    |> printfn "%s"

    // let userTest = 
    // login {
    //     Username = config.Authentication.Username;
    //     Password = config.Authentication.Password;
    // } |> ignore

    // printfn "%s" "whatever"

    // isLoggedInGuard()
    // |> printfn "%s" 

    // sendMessageTest()

    // produceMessage token
    // subscribeMq token

    // setup()
    // readInput()
    0 