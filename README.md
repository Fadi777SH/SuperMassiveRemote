##                                                                     SuperMassiveRemote
**About this project**

1 - this application let you fully control your remote computer via the internet .

2 - this is an ambitious project to help me understand the basics of networking , sockets , data transferring and data compression in which you create 
    an application and not just connect , but see and modify you personal or any computer you want want to connect to .
    
3 - it provide a pretty vivid image from the pc you want to connect with , and a smooth controlling which let you use 
    keystroks and all the possible clicks that you could provide from the mouse or the keyboard . 
    
**How the application works**
1 - the starting entry point of the app is by taking the IP address of you current machine and choosing by  either 
    being the remote (the one that gets controlled by) or being the acceptor who will see and control the remote desktop .
    
2 - you only pick one **single state** either remote or acceptor , in both terms you must specify
    the IP address of the second computer and they must explicitly type the IP address they want to connect with.


3 - when both of the computers provide the second point the application generate one sockets per computer  , in which it
    bind to the local IP address of the computer and connect to the assign IP address the been specified .
    
4 - you will not need to search inside you computer setting on you own IP address , the app will provide it to you . 
    you only need to get the IP address of the computer you want to connect with . so the remote type the IP het want to share the screen with , and 
    the acceptor type the IP of the remote computer he wants to control off .
    
**How the Sockets behaves**
1 -  one of the hardest part in which what is the fastest-low latency way to send and receive data from , I tried using TCP at 
      first due to the to the fact is it the most common , but finding out that UDP was more superior for my application made me switch the whole app to it instantly .
      
2 - once the application runs , the socket generate a Udp server and send and receive data by it .
3 - every move happen in the application via the acceptor get catch by the application and translate instantly to raw bytes and send via the socket
    and under the name of UDPMessage which is hold all the possible information to trigger the right move from the mouse or the keyboard .
4 - I used the FrameCapturepool under the name space of windows.grapphics to capture the screen from the remote computer at fastest as possible .
5 - for every frame that is capture , it will instantly compressed and convert to byte array and send to the acceptor  computer to process there under the name of UdpFrameMessage
    , the frame is not fully sent in one single message due to  the limitation of UDP message
    size (it is around 1400b -1500b  as I remember , but windows let you send a chunk with a size of 65kb at most per send operation ) , but 65kb still not enough 
    to send a single frame that might be 1080p in  resolution .
    
**How I send the Frame form the remote desktop to the acceptor**
1 - I declare a variable that contain all the frame splits of chunks and the single chunks 
    can't be higher than 64kb (this process happen after I compress the frame into bytes).
2 - I sent every chunks using a structure called udpframemessage , 
    this message contain the byte chunk itself , the total size of the frame ,
    the number of chunks in the frame , the chunk current number in the queue and a temporary random number as a fingerprint for the frame . 
3 - when the chunk arrive to the acceptor , it get modified by its data using a pretty lazy method to try requeue the frame and assemble the frame is it was .
4 - after the frame resembled in the acceptor in get display in the screen of him .

**How I simulate the mouse and keyboard hooks**
1 - when the acceptor move its mouse and clicks the keyboard , he expect to the same action to happen in the remote app .
2 - I take the type of action he did , convert it to raw bytes and sent it via the socket .
3 - the remote computer receive the message and translate it and uses a library called H.Inputsimulator to generate the the action depend on the message .

**How to use it**
_ you either go to "\SuperMassiveRemote\CToC\bin\Release\net10.0-windows10.0.20348.0\win-x64\SuperMassiveRemote.exe" or build it from 
  "\SuperMassiveRemote\SuperMassiveRemote.slnx"
    
