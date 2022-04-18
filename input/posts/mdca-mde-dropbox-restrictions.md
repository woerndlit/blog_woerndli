# Scenario

In the modern IT landscape it is important to know what cloud applications your users are using while also having the ability to block access to those apps if needed.

For that reason, Microsoft offers a Cloud Acceess Security Broker solution called Microsoft Defender for Cloud Apps (MDCA) as a part of their Microsoft Defender product line.

You can use MDCA to analyze cloud application usage of your users to sanction or unsaction those apps. You can also use MDCA monitor sessions and control up- and downloads to those applications based on multiple criterias by connecting the apps to your Azure AD and leveraging Azure ADs Conditional Access policies.

The downside of connecting a cloud app in that way is, that through this the cloud application is automatically sanctioned in MDCA, but the sessions are only monitored if the users are logging in with their Azure AD credentials into the cloud app. If a user chooses to login with private credentials, the sessions are not monitored and it is not possible to control what data is shared within these sessions.

The ideal remidiation to this issue is making sure to protect and encrypt your importand and confidential data.

In this article however, I'm pursuing the goal to allowing access to a Azure AD connected Dropbox Business instance while blocking the ability to share data to private dropbox accounts.

## Products used

To achieve this goal, I'm uinsg the following Microsoft solutions.

* Microsoft Defender for Cloud Apps (to monitor and control sessions to the connected Dropbox for Business instance)
* Microsoft Endpoint DLP (to limit upload and download possibilities to all other Dropbox instances)
