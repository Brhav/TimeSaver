# TimeSaver

Docker image to periodically save editions of the **De Tijd** newspaper to your Synology NAS.

`Note: An active reader subscription is required to use this tool. Please ensure you comply with all relevant legal requirements.`

## Docker

### Build Dockerfile (using Command Prompt)

- docker build --tag timesaver .
- docker save timesaver:latest | gzip > timesaver.tar.gz

### Add Docker Image to Synology NAS

- Docker -> Image -> Add -> Add From File -> Select timesaver.tar.gz

### Create Docker Container on Synology NAS

- Docker -> Container -> Create -> timesaver:latest
- Use the same network as the Docker Host
- Map volume /App/Config (Contains Settings.json, don't forget to enter user data!)
- Map volume /App/Downloads (Destination folder)
- Add environment variable TZ: Europe/Brussels

### Configure Cloud Sync on Synology NAS

- Cloud Sync doesn't detect files created by Docker:

	See https://kb.synology.com/en-global/DSM/help/CloudSync/cloudsync

	"Cloud Sync on DSM cannot instantly sync the file changes made on Docker DSM or other containers; likewise, Cloud Sync on Docker DSM or other containers cannot instantly sync the file changes made on DSM."

- Control Panel -> Task Scheduler -> Create -> Scheduled Task -> User-defined-script
	- Task: Restart Cloud Sync (This will trigger synchronisation)
	- User: root
	- Schedule: Daily at 07:05
	- User-defined script: /usr/syno/bin/synopkg restart CloudSync
