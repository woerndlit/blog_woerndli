# Prelimenary

In ConfigMgr OSD, I like to use the Modern Driver and BIOS Management Solution from the guys over at MSEndpointMgr.com ([Modern Driver Management - MSEndpointMgr](https://msendpointmgr.com/modern-driver-management/)) especially since they started to integrate the possibility to use XML logic files to identify the correct driver package during OSD.

To automate the process of creating this XML logic files, Maurice Daly added the possibility to create them with his Driver Automation Tool back in version 6.4.8
<br>

# Issue

Unfortunately in the current version 6.5.6 the creation of these XML logic files fails.
<br>

# Workaround

To remidiate this issue before Maurice fixes it in a future release, I've created a script based on his initial code to create the XML Logic Package independently from his Driver Automation Tool.

Please find the script in my GitHub gist below:

<script src="https://gist.github.com/woerndlit/992eb29916cb4f7f4e7819172232cb4b.js"></script>