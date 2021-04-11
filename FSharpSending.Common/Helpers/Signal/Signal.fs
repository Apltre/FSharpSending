namespace FSharpSending.Common.Helpers.Signal

open System.Threading.Channels

type CompletedSignalAwaiter = CompletedSignalAwaiter of ChannelReader<bool>
type EmitCompletedSignalFunc = EmitCompletedSignalFunc of (unit -> unit)

module CompletedSignalModule =
    
    let awaitCompleted (CompletedSignalAwaiter awaiter) = 
        async {
            let! success = (awaiter.WaitToReadAsync ()).AsTask () |> Async.AwaitTask
            return ()
        }

    let createDefault =
        let channelOptions = BoundedChannelOptions(capacity = 1, SingleReader = true, SingleWriter = true)
        let channel = Channel.CreateBounded<bool>(channelOptions)
        let readerChannel = channel.Reader
        let writerChannel = channel.Writer
        let signal (wc : ChannelWriter<bool>) () =
            wc.Complete ()
        (EmitCompletedSignalFunc (signal writerChannel), CompletedSignalAwaiter readerChannel)