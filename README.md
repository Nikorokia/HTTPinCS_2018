# Simple HTTP Server in C#
Project for CSCI 420: Computer Networking class at Houghton College, Spring 2018.

## Project Scope
"In this project you will explore the HTTP protocol by making an HTTP server directly with TCP in the language of your choice subject to my approval. Java, C#, python, C++, Rust, Scala, F#, and Haskell are all pre-approved. Your language will almost certainly have a library for creating an HTTP server directly, but we will not be using that. Instead you will create a program that accepts TCP connections and parses HTTP requests from clients that connect. You will have to handle GET, HEAD, and POST requests to successfully implement your server.

"Your server will implement an issue tracking website where users can visit the site with a browser and see a list of issues, add new issues, and mark issues as fixed. This must work for multiple users concurrently. Seeing the list of issues will be a `GET` request from the browser while adding new issues and marking issues will both be `POST` requests. For the `POST` requests the HTML you send will have a form that will submit using `POST`. The following HTML will direct the browser to use post to the url location `/submit.html` data from a multi-line text input area named `issue` when the `submit` button is pressed:
```
<form action="/submit.html" method="post">
  <label for="issue">New Issue:</label><br>
  <textarea name="issue" cols="50" rows="10"></textarea><br>
  <input type="submit">
</form>
```
"See the examples [here](https://www.w3schools.com/html/html_forms.asp) for more details.

"Perhaps the most challenging aspect of this project is handling concurrency of multiple clients visiting the website at the same time. Your solution to this will depend on the language you choose, but will require that you properly synchronize access to shared data. The easiest approach may be to use a single global lock which you will acquire before reading or write your issue data structure and release after."

## Project Submission
I chose to write my version in C#.

### Building:

#### Run without compiling:  
From the source base folder,
```
$ dotnet run {port}
```
_port defaults to 8080._


#### To compile:  
From the source base folder,
```
$ dotnet publish -c release
```
_Built files are in the "bin/release/netcoreapp2.0/publish/" folder._

### Running:

From the compiled files folder,
```
$ dotnet SimpleHttp.dll {port}
```
_port defaults to 8080._

### Optional:

The server was originally designed to use alternate html templates, though full funtionality is untested.  
Since this project is specifically an issue tracker, it will supply the tracking functionality wherever "<ISSUES_SECTION>" is included in the HTML code.
