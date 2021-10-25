open System
open System.Threading
open System.Text
open System.Text.Json
// open System.Net.Http
open RabbitMQ.Client
open RabbitMQ.Client.Events
open FSharp.Data

type Message = {
    messageId: int;
    rabbitUserId: int;
    timeSent: DateTime;
    messageContent: string;
    roomId: int;
}

let printWelcome () =
    printfn "%s" "********** WELCOME TO RABBIT CHAT **********"

let setup () = 
    printWelcome()
    printfn "%s" "Setting up application"

let handleInput input = 
    match input with
    | "test" -> printfn "%s" "fuck you"
    | _ -> printfn "%s" "fuck you anyway"

let handleShutdown () = Environment.Exit(0)

let getRoomMessages () =
    Http.RequestString("http://localhost:5000/api/message/4")
    |> JsonSerializer.Deserialize<Message[]>

let listFriends () = printfn "%s" "list"

let listTestRoomMessages () =
    let res = getRoomMessages()

    res
    |> Array.take 10
    |> Array.iter (fun message -> printfn "%s" message.messageContent)

let handleCommand input =
    printfn "%s" "processing command"
    match input with
    | "~quit" -> handleShutdown()
    | "~friends" -> listFriends()
    | "~testroom" -> listTestRoomMessages()
    | _ -> printfn "%s" input

let rec readInput () =
    printfn "%s" "waiting"
    let input = Console.ReadLine()
    match input with
    | input when input.StartsWith("~") -> handleCommand input 
    | _ -> handleInput input

    readInput()

let produceMessage (token : CancellationTokenSource) = 
    let hostName = "localhost"
    let exchange = "rabbit-test"
    let routingKey = ""
    let factory = ConnectionFactory(HostName = hostName, UserName = "guest", Password = "guest")
    use connection = factory.CreateConnection()
    use channel = connection.CreateModel()
    channel.ExchangeDeclare(exchange = exchange, ``type`` = ExchangeType.Fanout, durable = false) // , auto

    let rand = Random()

    while not token.IsCancellationRequested do
        let message = sprintf "%f" (rand.NextDouble())
        let body = Encoding.UTF8.GetBytes(message)
        printfn "publish: %s" message
        channel.BasicPublish(exchange = exchange, routingKey = routingKey, body = ReadOnlyMemory<byte>(body))
        Thread.Sleep(500)

let subscribeTest (token : CancellationTokenSource) =
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

[<EntryPoint>]
let main argv =
    let token = new CancellationTokenSource()
    token.CancelAfter 60000
    // produceMessage token
    subscribeTest token
    

    setup()
    readInput()
    0 