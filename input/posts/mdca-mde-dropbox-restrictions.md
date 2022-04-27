# Introduction

In the modern IT landscape it is important to know what cloud applications your users are using while also having the ability to block access to those apps if needed.

For that reason, Microsoft offers a Cloud Acceess Security Broker solution called Microsoft Defender for Cloud Apps (MDCA) as a part of their Microsoft Defender product line.

You can use MDCA to analyze cloud application usage of your users, sanction or unsaction those apps or control how your data can be used in those apps (depending on the implementation of the app into MDCA).
You can also use MDCA to monitor complete sessions and control up- and downloads to those applications based on multiple criterias by connecting the apps to your Azure AD and leveraging Azure ADs Conditional Access policies.

The downside of connecting a cloud app in that way is, that through this the cloud application is automatically sanctioned in MDCA, but the sessions are only monitored if the users are logging in with their Azure AD credentials into the cloud app. If a user chooses to login with private credentials, the sessions are not monitored and it is not possible to control what data is shared within these sessions.

The recommended remidiation to this issue is making sure to protect and encrypt your importand and confidential data.

In this article however, I'm pursuing the goal to allowing access to a Azure AD connected Dropbox Business instance while blocking the ability to share data to private dropbox accounts.

## Products used

To achieve this goal, I'm uinsg the following Microsoft products.

* Microsoft Defender for Cloud Apps (MDCA) (to monitor and control sessions to the connected Dropbox for Business instance)
* Microsoft Endpoint DLP (to limit upload and download possibilities to all other Dropbox instances)

- - -

## Microsoft Defender for Cloud Apps: Connect Dropbox Business for Session Control

### Connect Dropbox to AzureAD for SSO

First, we need to connect the Dropbox for Business instance to Azure AD for single sign-on.

1\. Go to the Enterprise applications blade in Azure AD and add a new application ([https://portal.azure.com/#blade/Microsoft\_AAD\_IAM/StartboardApplicationsMenuBlade/AppAppsPreview/menuId/](https://portal.azure.com/#blade/Microsoft_AAD_IAM/StartboardApplicationsMenuBlade/AppAppsPreview/menuId/))

![image.png](.media/img_23.png)

2\. Search for Dropbox Business, select it and click Create

![image.png](.media/img_24.png)

3\. When the app is created, go to the Single sign-on blade and choose SAML

![image.png](.media/img_25.png)

4\. Edit the basic SAML Configuration

![image.png](.media/img_26.png)

5\. Add "[https://www.dropbox.com/saml\_login](https://www.dropbox.com/saml_login)" as a Reply URL and add the Sign on URL which you can find in the Single sign-on configuration of your Dropbox Business account.

![image.png](.media/img_27.png)

![image.png](.media/img_28.png)

6\. Download the SAML Signing Certificate and upload it in the SSO settings of your Dropbox Business account

![image.png](.media/img_29.png)

![image.png](.media/img_30.png)

7\. Copy the Login URL and add it as the Identity provider sign-in URL in the SSO settings of your Dropbox Business account.

![image.png](.media/img_31.png)

8\. ![image.png](.media/img_32.png)

9\. To finish the configuration, add the users or groups which will have access to this application

![image.png](.media/img_33.png)

- - -

### Create Conditional Access Session Control Policy

1\. Go to Azure AD Conditional Access in the Azure Portal ([Conditional Access - Microsoft Azure](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ConditionalAccessBlade/Policies))

2\. Create a new Policy called "CA001 - Dropbox Business Session Control"

3\. Select users or groups to assign this policy to

![image.png](.media/img_34.png)

4\. Selecct Dropbox Business

![image.png](.media/img_35.png)

5\. Choose "Use Conditional Access App Control" in Session Control and select "Use custom policy..."

![image.png](.media/img_36.png)

- - -

### Create Conditional Access Policy in Microsoft Defender for Cloud Apps

1\. Go to the [MDCA Portal](https://portal.cloudappsecurity.com)

2\. Navigate to Conditional access in Control -> Policies and create a new policy called: "SP01 - Monitor Dropbox"

![image.png](.media/img_37.png)

3\. Filter the Acitivity source to Dropbox

![image.png](.media/img_38.png)

4\. Set the session control type to Monitor only.
*Note: Here you can also control the the activities performed within your connected Dropbox instances, but this is not the scope of this article.*

![image.png](.media/img_39.png)

5\. Save the policy

- - -

### Result

If we now navigate to Discovered Apps in MDCA portal, we see that Dropbox has been sanctioned as a cloud app and we cannot unsaction it as long as it is connected.

![image.png](.media/img_40.png)

After logging into Dropbox with our Azure AD Credentials, we see that MDCA is redirecting our traffic throug a reverse proxy to monitor the Dropbox traffic.

![image.png](.media/img_41.png)
If we login with a private credentials to another Dropbox instance, traffic is still going directly to [www.dropbox.com](http://www.dropbox.com) and cannot be controlled.

- - -

## Microsoft Endpoint DLP: Limit Upload capabilities to Dropbox

### Configure service domain restrictions

1\. Go to the [Microsoft Compliance Portal](https://compliance.microsoft.com)

2\. Choose Endpoint DLP Settings in the Data loss prevention blade

![image.png](.media/img_42.png)

3\. In "Browser and domian restrictions to sensitive data" choose "Block" for service domains and add the Dropbox domains. (In production you would change this to "Allow" service domains and only allow your explicitly sanctioned domains)

![image.png](.media/img_43.png)

- - -

### Create Data loss prevention policy

1\. Create a new custom policy named DLP001 - Dropbox restrictions

![image.png](.media/img_44.png)

2\. Apply the policy to Devices only

![image.png](.media/img_45.png)

3\. Create 2 custom rules:
\- DLPR001 \- File Type \-\> Detects content to restrict by file type

![image.png](.media/img_46.png)

\- DLPR002 \- File extension \-\> Detects content to restrict by file extension

![image.png](.media/img_47.png)

Block Service domain and browser activities for both of them:

![img_48.png](.media/img_52.png)![image.png](.media/img_48.png)

4\. Save the policy and wait up to an hour for the new policy to becom active.

- - -
## Result

When logging in to our Dropbox business account, we can still upload documents

![image.png](.media/img_49.png)

![image.png](.media/img_50.png)

When logging in with a private Dropbox account, upload of documents is restricted based on the service domain.

![image.png](.media/img_51.png)

## Conclusion

In my opinion, Microsofts security products provide already great value if you implement them one at a time. If you start to combine them to solutions for your specific scenarios however, that is when you get the most benefit out of them.