#!/bin/sh
### BEGIN INIT INFO
# Provides:          Push_Notification_Server
# Short-Description: Server for deploying push notifications to users
# Required-Start: 
# Required-Stop:  
# Default-Start:  
# Default-Stop:   
# Description:       This server serves push notificaitons to asset users
#					 on port 3010
### END INIT INFO
sudo mono PushNotificationServer/PushNotificationServer.exe -t 10
