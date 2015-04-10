# ShibpurConnect
[IIEST] (http://en.wikipedia.org/wiki/Indian_Institute_of_Engineering_Science_and_Technology,_Shibpur) student's welfare web-application, see our progress here http://shibpur.azurewebsites.net/

### Git Configuration:
Need to do it only once and if you haven't used Git before

1. Install [Git](http://git-scm.com/downloads)  
2. Install [Sourcetree](http://www.sourcetreeapp.com/) to do all Git operations 

### Clone Application:

* Create a new folder in your machine. Open command line and do below steps
```
cd 'new directory path'
git clone https://github.com/pritamkarmakar/shibpurconnect.git
```
* open 'ShibpurConnect.sln' file using Visual Studio
* if you see any error with dll reference run below command from Package Manager Console
```
Update-Package -Reinstall
```


## Do you want to get rid of password ask from gitbucket for each pull/push then follow below steps ->
 
1.Open Git-bash (search in windows all programs) and generate a new SSH key using below command
```
ssh-keygen -t rsa -C "youremail"
```
2.it will ask for passphrase, you can keep it empty

3.after this process you will get a private and public key in C:\Users\<username>\.ssh folder. id_rsa is the private key and id_rsa.pub is your public key. **Never share your private key to anyone**.

4.Now open the .pub file in notepad copy the content go to this page [https://bitbucket.org/pritam83/shibpurconnect/admin/deploy-keys] and create a ney key using the .pub file content 

Do git pull/push, you shouldn't be asked for the password anymore.