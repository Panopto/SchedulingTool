Scheduling Tool
=====================

Scheduling Tool

This program provides a GUI for users to schedule multiple remote recordings using a XML file containing recording information. User can also use this program to Bulk Delete multiple recordings at once by selecting and deleting recordings from search results obtained by searching after filling in provided fields with appropriate information.

Example of usage of available tags are located in sample.xml.

The server this program is directed towards can be changed in the app.config file. Change the app.config files located in PanoptoScheduleUploader.Services and PanoptoScheduleUploader.UI folder. Change the server location in the address attribute inside endpoint tag under client tag.

To change the server the executable is directed towards, change the client tag's endpoint tag's address attribute to desired server in Panopto Schedule Uploader.exe.config file located at PanoptoScheduleUploader.UI\bin\Debug.

	User Name: user name to log into server with
	Password: password for provided user name

Scheduler:

	File: XML file containing remote recording to be scheduled
	Open: opens file selection interface
	Overwrite Existing Recordings: removes all existing recordings in conflict with recordings to be scheduled
	Submit: schedule recordings using given XML file

Bulk Delete:

	From: lower bound of the range of dates to search recordings in
	To: upper bound of the range of dates to search recordings in
	Number of Views: search for recordings with number of views strictly less than input of this field
	Minutes Viewed: search for recordings with minutes viewed strictly less than input of this field
	Unique Visitors: search for recordings with unique visitors strictly less than input of this field
	Search: start searching using the given criteria
	Delete Selected: delete recordings checked by user in the search results on the right
