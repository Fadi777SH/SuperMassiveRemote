##                                                                     SuperMassiveRemote
**About this project**

1 - this application lets you fully control your remote computer via the internet .

2 - this is an ambitious project to help me understand the basics of networking , sockets , data transferring and data compression in which you create 
    an application that not just connect , but see and modify your personal computer or any computer you want to connect with.
    
3 - it provides a pretty vivid image from the pc you want to connect with , and a smooth controlling which lets you use 
    shortcuts and all the possible clicks that you could provide from your mouse or the keyboard . 
    
**How the application works**

1 - the starting entry point of the app is by taking the IP address of your current machine and then select by either 
    being the remote (the one that gets controlled by) or being the acceptor who will see and control the remote desktop.
    
2 - you only pick one **single state** either remote or acceptor , in both terms you must specify
    the IP address of the second computer and they must explicitly type the IP address both the computers want to connect with.

3 - when both of the computers provide the second point above the application generate one sockets per computer, in which it
    bind to the local IP address of the computer and connect to the assign IP address the been specified .
    
4 - you will not need to search inside you computer setting on your own IP address, the app will provide it to you . 
    you only need to get the IP address of the computer you want to connect with . so the remote type the IP he want to share the screen with , and 
    the acceptor type the IP of the remote computer he wants to control off.
    
**How the Sockets behaves**

1 -  one of the hardest part in which what is the fastest-low latency way to send and receive data from , I tried using TCP at 
      first due to the to the fact is it the most common , but finding out that UDP was more superior for my application made me switch the whole app to it instantly .
      
2 - once the application runs , the socket generate a Udp server and send and receive data through it.
3 - every move happens in the application via the acceptor get catch by the application and translate instantly to raw bytes and send via the socket
    under the name of UDPMessage which is hold all the possible information to trigger the right move from the mouse or the keyboard.
4 - I used the name space of windows.graphics to capture the screen from the remote computer as fastest as possible.
5 - for every frame that is capture , it will instantly compressed and convert to byte array and send to the acceptor computer to process there under the name of UdpFrameMessage
    , pointing that the frame is not fully sent in one single message due to the limitation of UDP message
    size (it is around 1400b -1500b as I remember ,but windows let you send a chunk with a size of 65kb at most per send operation ) , but 65kb still not enough 
    to send a single frame that might be 1080p(or higher) resolution.
    
**How I send the Frame form the remote desktop to the acceptor**

1 - after compressing the frame using LZ4Codec and convert it into byte array , I split the array into chunks and put it inside a variable (the single chunk 
    can't be higher than 64kb).
2 - I sent every chunks using a structure called udpframemessage , 
    this message contain the byte chunk itself , the total size of the frame ,
    the number of chunks in the frame , the chunk current number in the queue and a temporary random number as a fingerprint for the frame . 
3 - when the chunk arrive to the acceptor , it get modified by its data using a pretty lazy method to try requeue the frame and resemble the frame is it was .
4 - after the frame resembled in the acceptor in get display in the frame windows I made using WPF.

**How I simulate the mouse and keyboard hooks**

1 - when the acceptor move its mouse and clicks the keyboard , he expects to the same action to happen in the remote computer.
2 - I take the type of action he did(using the events from WPF) , convert it to raw bytes and sent it via the socket .
3 - the remote computer receive the message and translate it and uses a library called H.Inputsimulator to generate the the action depend on the message .

**Real numbers**

1 - the app provide a pretty decent result form memory management to FPS , it gives about 15 frame if your computer is at 4k and more than 30 FPS if it at 720p due to the proccessor streangth and internet speed.
2 - about RAM, it never used more than 200-300mb at extensive usage.
3 -  I personally support use it for simple objectives like browsing, trying to fix your friends problem with there computers or send files , but not for gaming (see the last point).
**How to use it**

_ you either go to "\SuperMassiveRemote\CToC\bin\Release\net10.0-windows10.0.20348.0\win-x64\SuperMassiveRemote.exe" or build it from 
  "\SuperMassiveRemote\SuperMassiveRemote.slnx"

**Future plane**

1 - get rid of LZ4Codec compression method and replace it with a hardware compression method like H.265 or H.264.
   even though LZ4Codec is far from being slow (a single frame at 4k needs about 30ms to compress and at 1080p needs 11-15ms), unfortunately as I said before the app is does not suit gaming because the       compression method needs to be var faster than what LZ4Codec provies.
2 - change the UI of the app; make it look better , even tough the app looks decent, but it would be better if I change the colors ad the buttons and there animation.


