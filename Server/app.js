var http = require('http');
var path = require('path');
var express = require('express');
var session = require('express-session');

var app = express();

app.set('views', path.resolve(__dirname, 'views'));
app.set('view engine', 'ejs');

app.get("/", function(request,response){
	MongoClient.connect(url, function(err,db){
		if(err)throw err;
		var dbObj= db.db("SocketGameData");
		
		dbObj.collection("playerData").find({}).toArray(function(err, results){
			console.log("Site Served. Data:\n" + JSON.stringify(results[0], null, 4));

			db.close();
			response.render("index", {entries:results[0]});
		});		
	});	
});

app.get("/highscores", function(request,response){
	MongoClient.connect(url, function(err,db){
		if(err)throw err;
		var dbObj= db.db("SocketGameData");
		
		dbObj.collection("highscores").find({}).toArray(function(err, results){
			console.log("Site Served. Scores:\n" + JSON.stringify(results[0], null, 4));

			db.close();
			response.render("highscores", {entries:results[0]});
		});		
	});	
});

app.use(function(request, response){
	response.status(404).render("404");
});

http.createServer(app).listen(3000, function(){
	console.log("Quiz game webapp started on port 3000");
});


var io = require('socket.io')(process.envPort||5000);
var shortid = require('shortid');
var MongoClient = require('mongodb').MongoClient;
var url = "mongodb://localhost:27017/";

console.log("Quiz game server started on port 5000\n");

var dbObj;

MongoClient.connect(url, function(err, client)
{
	if(err)throw err;
	
	dbObj = client.db("SocketGameData");
});

io.on('connection', function(socket)
{	
	socket.on('ping', function(data)
	{
		socket.broadcast.emit('connect', data);
		socket.emit('connect', data);
	});

	socket.on('send data', function(data)
	{
		console.log("Received data from client. Data:")
		console.log(JSON.stringify(data, null, 4) + "\n");

		dbObj.collection("playerData").save(data, function(err, res)
		{
			if(err)throw err;
			console.log("Data saved to server.\n");
		});
	});

	socket.on('send highscores', function(data)
	{
		console.log("Received highscores from client. Scores:")
		console.log(JSON.stringify(data, null, 4) + "\n");

		dbObj.collection("highscores").remove({}, function(err, obj) {
			if (err) throw err;
			console.log(obj.result.n + " document(s) deleted");

			dbObj.collection("highscores").save(data, function(err, res)
			{
				if(err)throw err;
				console.log("Highscores saved to server.\n");
			});
		});
	});

	socket.on('load data', function(data)
	{		
		dbObj.collection("playerData").find({}).toArray(function(err, result) 
		{
			if (err) throw err;

			console.log("Sent data to client. Data: ");
			console.log(JSON.stringify(result, null, 4) + "\n");

			socket.broadcast.emit('receiveServerData', result[0]);
			socket.emit('receiveServerData', result[0]);
		});
	});

	socket.on('load highscores', function(data)
	{		
		dbObj.collection("highscores").find({}).toArray(function(err, result) 
		{
			if (err) throw err;

			console.log("Sent highscores to client. Scores: ");
			console.log(JSON.stringify(result, null, 4) + "\n");

			socket.broadcast.emit('receiveHighscores', result[0]);
			socket.emit('receiveHighscores', result[0]);
		});
	});
});
