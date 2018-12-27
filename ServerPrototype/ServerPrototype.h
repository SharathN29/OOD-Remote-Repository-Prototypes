#pragma once
///////////////////////////////////////////////////////////////////////
// ServerPrototype.h - This package processes the incoming message   //
// ver 1.0                                                           //
//Author- Sharath Nagendra			                                 //
//Source- Jim Fawcett                                                //
//CSE687 - Object Oriented Design, Spring 2018                       //
///////////////////////////////////////////////////////////////////////
/*
*  Package Operations:
* ---------------------
*  This package contains one class called Server which has a msg passing communication facility. 
* It processes every single message by invoking the installed callable obj defined by msg command key.
*
* The server main thread is free to do any necessary background work as the msg handling is being taken care 
* by the child thread. 
*
*  Required Files:
* -----------------
*  ServerPrototype.h, ServerPrototype.cpp
*  Comm.h, Comm.cpp, IComm.h
*  Message.h, Message.cpp
*  FileSystem.h, FileSystem.cpp
*  Utilities.h
*
*  Maintenance History:
* ----------------------
*  ver 1.0 - 4/17/2018
*  - first release
*/
#include <vector>
#include <string>
#include <unordered_map>
#include <functional>
#include <thread>
#include "../CppCommWithFileXfer/Message/Message.h"
#include "../CppCommWithFileXfer/MsgPassingComm/Comm.h"
#include <windows.h>
#include <tchar.h>

namespace Repository
{
  using File = std::string;
  using Files = std::vector<File>;
  using Direct = std::string;
  using Directs = std::vector<Direct>;
  using SearchPath = std::string;
  using Key = std::string;
  using Messg = MsgPassingCommunication::Message;
  using ServerProc = std::function<Messg(Messg)>;
  using MsgDispatcher = std::unordered_map<Key,ServerProc>;
  
  const SearchPath stgRoot = "../Storage";  // root directory for all server file storage
  const MsgPassingCommunication::EndPoint serverEndPoint("localhost", 8080);  // point where msgs are listenend 

  class Server
  {
  public:
    Server(MsgPassingCommunication::EndPoint end, const std::string& name);
    void start();
    void stop();
    void addMsgProcess(Key key, ServerProc proc);
    void procMessages();
    void postMessage(MsgPassingCommunication::Message msg);
    MsgPassingCommunication::Message getMessage();
    static Directs getDirs(const SearchPath& path = stgRoot);
    static Files getFiles(const SearchPath& path = stgRoot);
	
  private:
    MsgPassingCommunication::Comm comm_;
    MsgDispatcher dispatch_;
    std::thread messgProcessThrd_;
  };
  // Function to initiatize end point of a server and give the server a name

  inline Server::Server(MsgPassingCommunication::EndPoint end, const std::string& name)
    : comm_(end, name) {}

  // Function to start server's instance of communication 

  inline void Server::start()
  {
    comm_.start();
  }
  // Function to stop an instance of communication 

  inline void Server::stop()
  {
    if(messgProcessThrd_.joinable())
      messgProcessThrd_.join();
    comm_.stop();
  }
  // Function to pass msgs to communication channel for sending 

  inline void Server::postMessage(MsgPassingCommunication::Message messg)
  {
    comm_.postMessage(messg);
  }
  // Function to get msg from Communication channel 

  inline MsgPassingCommunication::Message Server::getMessage()
  {
    Messg messg = comm_.getMessage();
    return messg;
  }
  // Function to add ServerProc callable object to server's dispatcher  

  inline void Server::addMsgProcess(Key key, ServerProc proc)
  {
    dispatch_[key] = proc;
  }
  // Function to start processing msgs on a child thread

  inline void Server::procMessages()
  {
    auto proc = [&]()
    {
      if (dispatch_.size() == 0)
      {
        std::cout << "\n  There are no server processes to call";
        return;
      }
      while (true)
      {
        Messg messg = getMessage();
        std::cout << "\n  Message Received: " << messg.command() << " from " << messg.from().toString();
        if (messg.containsKey("verbose"))
        {
          std::cout << "\n";
          messg.show();
        }
        if (messg.command() == "quitServer")
          break;
        Messg reply = dispatch_[messg.command()](messg);
        if (messg.to().port != messg.from().port) 
        {
          postMessage(reply);
		  if (reply.containsKey("file"))
		  {
			  Messg fileRep;
			  fileRep.to(messg.from());
			  fileRep.from(messg.to());
			  fileRep.command("filesent");
			  fileRep.attribute("fileName", reply.value("file"));
			  postMessage(fileRep);
		  }
          messg.show();
          reply.show();
        }
        else
          std::cout << "\n Server is attempting to post to self ";
      }
      std::cout << "\n Server message which is processing thread is being shut down ";
    };
    std::thread t(proc);
    std::cout << "\n  starting server thread to process messages";
    messgProcessThrd_ = std::move(t);
  }
}