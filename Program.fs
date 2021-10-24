open System
open System.Threading
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events

let setup () = 
    printfn "%s" "Setting up application"

let handleInput input = 
    match input with
    | "test" -> printfn "%s" "fuck you"
    | _ -> printfn "%s" "fuck you anyway"

let handleShutdown () = Environment.Exit(0)

let handleCommand input =
    printfn "%s" "processing command"
    match input with
    | "~quit" -> handleShutdown()
    | _ -> printfn "%s" input

let rec readInput () =
    printfn "%s" "waiting"
    let input = Console.ReadLine()
    match input with
    | input when input.StartsWith("~") -> handleCommand input 
    | _ -> handleInput input

    readInput()

let rec readInputAsync () = async {
    printfn "%s" "waiting"
    let input = Console.ReadLine()
    match input with
    | input when input.StartsWith("~") -> handleCommand input 
    | _ -> handleInput input

    readInputAsync() 
}

let processCommand command =
    command

// RABBIT MQ TESTS

let produceMessage hostName exchange routingKey (token : CancellationTokenSource) = async {
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

    printfn "%s" "Closing connection"
}

let produceMessageSync hostName exchange routingKey (token : CancellationTokenSource) = 
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

[<EntryPoint>]
let main argv =
    let host = "localhost"
    let exchange = "rabbit-test"
    let routingKey = ""

    //  async {
    //      let token = new CancellationTokenSource()
    //      token.CancelAfter 5000
    //      produceMessage host exchange routingKey token
    //  } 

    //  readInputAsync()
    //  Console.ReadKey()

    // let token = new CancellationTokenSource()
    // token.CancelAfter 5000
    // produceMessage host exchange routingKey token

    // setup()
    // readInput()
    0 