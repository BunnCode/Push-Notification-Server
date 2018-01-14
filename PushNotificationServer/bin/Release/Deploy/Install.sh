sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/ubuntu xenial main" | sudo tee /etc/apt/sources.list.d/mono-official.list
sudo apt-get update
sudo apt-get install mono-devel
cp -a ./PushNotificationServer/. /usr/local/PushNotificationServer/
chmod +x ./RunPushServer.sh
cp ./RunPushServer.sh /etc/init.d/RunPushServer.sh
update-rc.d RunPushServer.sh start