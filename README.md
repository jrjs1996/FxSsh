## FxSsh Reverse Server
This is an extension of FxSsh (https://github.com/Aimeast/FxSsh). It adds functionality for accepting reverse port forwarding (https://blog.devolutions.net/2017/3/what-is-reverse-ssh-port-forwarding) connections.

---

### Testing

The MIT license

Testing:

Start the SshServerLoader on the windows machine.

On the linux machine run the command ssh -R 8000:localhost:8000 root@169.254.73.253
(Replace the ip after root@ with your the IP on your windows machine)

You should see something similar to this on your SshServerLoader console:

Accepted a client.
Session E0186DF95A42425A78AEF277FB45F0B31BAB42E5 requesting UserauthService.
Session E0186DF95A42425A78AEF277FB45F0B31BAB42E5 requesting ConnectionService.
Channel 0 runs command: "shell".

You should now have a reverse connection to the client.

---
