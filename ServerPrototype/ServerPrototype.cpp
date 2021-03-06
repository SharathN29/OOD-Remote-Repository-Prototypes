/////////////////////////////////////////////////////////////////////////
// ServerPrototype.cpp - This package processes the incoming message   //
// ver 1.0								                               //
//Author- Sharath Nagendra			  	                               //
//Source- Jim Fawcett												   //
//CSE687 - Object Oriented Design, Spring 2018                         //
/////////////////////////////////////////////////////////////////////////

#include "ServerPrototype.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include <chrono>

namespace MsgPassComm = MsgPassingCommunication;

using namespace Repository;
using namespace FileSystem;
using Messg = MsgPassingCommunication::Message;

Files Server::getFiles(const Repository::SearchPath& path)
{
	return Directory::getFiles(path);
}

Directs Server::getDirs(const Repository::SearchPath& path)
{
	return Directory::getDirectories(path);
}

template<typename T>
void show(const T& t, const std::string& messg)
{
	std::cout << "\n  " << messg.c_str();
	for (auto item : t)
	{
		std::cout << "\n    " << item.c_str();
	}
}

std::function<Messg(Messg)> echo = [](Messg messg) {
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.remove("message");
	re.attribute("message", "Hello " + messg.value("message"));
	return re;
};

std::function<Messg(Messg)> Checkinfile = [](Messg messg)
{
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.command("CheckinAcknowledgement");
	re.attribute("message", "DoneWithCheckin");
	return re;

};



std::function<Messg(Messg)> Checkoutfile = [](Messg messg)
{
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.command("CheckoutAcknowledgement");
	re.attribute("message", "DoneWithCheckOut");
	return re;

};


std::function<Messg(Messg)> viewmeta = [](Messg messg)
{
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.command("MetadataAcknowledgement");
	re.attribute("message", "SentMetadata");
	re.attribute("description", " Dummy Metadata");
	re.attribute("date", "04/15/2018");
	re.attribute("path", "C:\\Users\\Sharath Nagendra\\Downloads\\Project3\\Storage\\Socket.h");
	re.attribute("status", "Close");

	return re;

};


std::function<Messg(Messg)> getversion = [](Messg messg)
{
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.command("VersionAcknowledgement");
	re.attribute("message", "5");
	return re;

};

std::function<Messg(Messg)> getFile = [](Messg messg) {
	Messg re;
	re.to(messg.from());
	re.from(messg.to());
	re.command("getFile");
	std::string path = messg.value("path");
	size_t pos = path.find_last_of("/");
	std::string fileName = path.substr(pos + 1, path.length());
	std::string dest = "../CppCommWithFileXfer/SendFiles/" + fileName;
	FileSystem::File::copy(path, dest);
	re.attribute("file", fileName);

	return re;
};

std::function<Messg(Messg)> connection = [](Messg messg) {
	Messg re = messg;
	re.to(messg.from());
	re.from(messg.to());
	re.attribute("message", "Connected");
	return re;
};

std::function<Messg(Messg)> getFiles = [](Messg messg) {
	Messg re;
	re.to(messg.from());
	re.from(messg.to());
	re.command("getFiles");
	std::string path = messg.value("path");
	if (path != "")
	{
		std::string searchPath = stgRoot;
		if (path != ".")
			searchPath = searchPath + "\\" + path;
		Files files = Server::getFiles(searchPath);
		size_t count = 0;
		for (auto item : files)
		{
			std::string countStr = Utilities::Converter<size_t>::toString(++count);
			re.attribute("file" + countStr, item);
		}
	}
	else
	{
		std::cout << "\n Path attribute could not be defined with getFiles message";
	}
	return re;
};

std::function<Messg(Messg)> getDirs = [](Messg messg) {
	Messg re;
	re.to(messg.from());
	re.from(messg.to());
	re.command("getDirs");
	std::string path = messg.value("path");
	if (path != "")
	{
		std::string searchPath = stgRoot;
		if (path != ".")
			searchPath = searchPath + "\\" + path;
		Files dirs = Server::getDirs(searchPath);
		size_t count = 0;
		for (auto item : dirs)
		{
			if (item != ".." && item != ".")
			{
				std::string countStr = Utilities::Converter<size_t>::toString(++count);
				re.attribute("dir" + countStr, item);
			}
		}
	}
	else
	{
		std::cout << "\n  Path attribute could not be defined with getDirs message";
	}
	return re;
};




int main()
{
	std::cout << "\n  Testing Server Prototype\n ==========================\n\n Testing the methods to get files and directories \n-------------------------------------------------\n\n  Checking msg processing \n ----------------------------";
	Server server(serverEndPoint, "ServerPrototype");
	server.start();
	Files files = server.getFiles();
	show(files, "Files:");
	Directs dirs = server.getDirs();
	show(dirs, "Directs:");
	server.addMsgProcess("getFile", getFile);
	server.addMsgProcess("Checkoutfile", Checkoutfile);
	server.addMsgProcess("viewmeta", viewmeta);
	server.addMsgProcess("getversion", getversion);
	server.addMsgProcess("Checkoutfile", Checkoutfile);
	server.addMsgProcess("Checkinfile", Checkinfile);
	server.addMsgProcess("echo", echo);
	server.addMsgProcess("getFiles", getFiles);
	server.addMsgProcess("getDirs", getDirs);
	server.addMsgProcess("serverQuit", echo);
	server.addMsgProcess("connection", connection);
	server.procMessages();
	Messg msg(serverEndPoint, serverEndPoint);  // send to self
	msg.name("msgToSelf");
	msg.command("echo");
	msg.attribute("verbose", "show me");
	server.postMessage(msg);
	std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	msg.command("getFiles");
	msg.remove("verbose");
	msg.attributes()["path"] = stgRoot;
	server.postMessage(msg);
	std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	msg.command("getDirs");
	msg.attributes()["path"] = stgRoot;
	server.postMessage(msg);
	std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	std::cout << "\n  press enter to exit";
	std::cin.get();
	msg.command("serverQuit");
	server.postMessage(msg);
	server.stop();
	return 0;
}

