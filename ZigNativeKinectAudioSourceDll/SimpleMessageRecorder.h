#pragma once

#include <string>

using namespace std;


class SimpleMessageRecorder
{
public:

	SimpleMessageRecorder(string msgHeader)
	{
		m_msgHeader = msgHeader;
		m_message = m_msgHeader;
	}
	
	void AppendToMessage(string message)
	{
		m_message.append(message + "\n");
	}
	void AppendToMessage(string message, const char* arg)
	{
		char m[200];
		sprintf_s(m, message.c_str(), arg);
		AppendToMessage(m);
	}
	void AppendToMessage(string message, string arg)
	{
		AppendToMessage(message, arg.c_str());
	}
	void AppendToMessage(string message, int arg)
	{
		char m[200];
		sprintf_s(m, message.c_str(), arg);
		AppendToMessage(m);
	}
	void AppendToMessage(string message, float arg)
	{
		char m[200];
		sprintf_s(m, message.c_str(), arg);
		AppendToMessage(m);
	}
	void AppendToMessage(string message, bool arg)
	{
		string argStr = arg ? "true" : "false";
		AppendToMessage(message, argStr);
	}

	const char* GetLastRecordedMessage(bool doClearMessage = false)
	{
		static string msg;
		msg = m_message;
		if(doClearMessage){ ClearLastRecordedMessage(); }
		return msg.c_str();
	}

private:

	string m_msgHeader;
	string m_message;

	void ClearLastRecordedMessage()
	{
		m_message = m_msgHeader;
	}

};

