using System.Collections.Generic;
using AdminShellNS;
/*
Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
Copyright (c) 2018-2020 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
*/

namespace AasxServer
{
    public class I40SemanticKey
    {
        public string type;
        public string local;
        public string value;
        public string idType;
    }
    public class I40SemanticProtocol
    {
        public List<I40SemanticKey> keys;
        public I40SemanticProtocol()
        {
            keys = new List<I40SemanticKey>();
        }
    }
    public class I40Identification
    {
        public string id;
        public string idType;
    }
    public class I40role
    {
        public string name;
    }
    public class I40EndPointID
    {
        public I40Identification identification;
        public I40role role;
    }

    public class I40TransmitFrame
    {
        public I40SemanticProtocol semanticProtocol;
        public I40EndPointID sender;
        public I40EndPointID receiver;
        public string type; //CFP, proposal, acceptporposal, Nested
        public string messageId;
        public string replyBy;
        public string replyTo;
        public string conversationId;
    }

    public class I40Message
    {
        public I40TransmitFrame frame;
        public List<string> interactionElements;
        public I40Message()
        {
            interactionElements = new List<string> { };
        }

    }

    public class I40Message_Interaction
    {
        public I40TransmitFrame frame;
        public List<AdminShellNS.AdminShell.Submodel> interactionElements;
        public I40Message_Interaction()
        {
            interactionElements = new List<AdminShellNS.AdminShell.Submodel> { };
        }

    }

    public class I40MessageHelper
    {
        public I40Message createConnectProtMessage(string connectNodeName)
        {
            I40Message _i40Message = new I40Message();
            I40TransmitFrame i40Frame = new I40TransmitFrame();
            i40Frame.type = "HeartBeat";
            i40Frame.replyBy = "RESTAPI";
            i40Frame.replyTo = "RESTAPI";

            I40EndPointID sender = new I40EndPointID();
            I40EndPointID receiver = new I40EndPointID();
            I40Identification seID = new I40Identification();
            I40role seRole = new I40role();

            I40Identification reID = new I40Identification();
            I40role reRole = new I40role();

            seID.id = connectNodeName;
            seID.idType = "idShort";
            seRole.name = "AASXServerConnect";
            sender.identification = seID;
            sender.role = seRole;
            i40Frame.sender = sender;

            reID.id = "VWS_RIC";
            reID.idType = "idShort";
            reRole.name = "ConnectProtocol";
            receiver.identification = reID;
            receiver.role = reRole;
            i40Frame.receiver = receiver;

            I40SemanticKey i40Key = new I40SemanticKey();
            i40Key.type = "GlobalReference";
            i40Key.local = "local";
            i40Key.value = "heartbeat";
            i40Key.idType = "False";

            I40SemanticProtocol semanticProtocol = new I40SemanticProtocol();
            semanticProtocol.keys.Add(i40Key);

            i40Frame.semanticProtocol = semanticProtocol;
            i40Frame.messageId = connectNodeName + 1;

            _i40Message.frame = i40Frame;

            return _i40Message;
        }
        public I40Message createDescriptorMessage(string connectNodeName)
        {
            I40Message _i40Message = new I40Message();
            I40TransmitFrame i40Frame = new I40TransmitFrame();
            i40Frame.type = "register";
            i40Frame.replyBy = "RESTAPI";
            i40Frame.replyTo = "RESTAPI";

            I40EndPointID sender = new I40EndPointID();
            I40EndPointID receiver = new I40EndPointID();
            I40Identification seID = new I40Identification();
            I40role seRole = new I40role();

            I40Identification reID = new I40Identification();
            I40role reRole = new I40role();

            seID.id = connectNodeName;
            seID.idType = "idShort";
            seRole.name = "AASXServerConnect";
            sender.identification = seID;
            sender.role = seRole;
            i40Frame.sender = sender;

            reID.id = "VWS_RIC";
            reID.idType = "idShort";
            reRole.name = "RegistryHandler";
            receiver.identification = reID;
            receiver.role = reRole;
            i40Frame.receiver = receiver;

            I40SemanticKey i40Key = new I40SemanticKey();
            i40Key.type = "GlobalReference";
            i40Key.local = "local";
            i40Key.value = "registration";
            i40Key.idType = "False";

            I40SemanticProtocol semanticProtocol = new I40SemanticProtocol();
            semanticProtocol.keys.Add(i40Key);

            i40Frame.semanticProtocol = semanticProtocol;
            i40Frame.messageId = connectNodeName + 1;
            _i40Message.frame = i40Frame;

            return _i40Message;
        }

        public I40Message createInteractionMessage(string connectNodeName,
                      string receiverId, string receiverRole, string senderRole, string messageType,
                      string replyBy, string replyTo)
        {
            I40Message _i40Message = new I40Message();
            I40TransmitFrame i40Frame = new I40TransmitFrame();
            i40Frame.type = messageType;
            i40Frame.replyBy = replyBy;
            i40Frame.replyTo = replyTo;

            I40EndPointID sender = new I40EndPointID();
            I40EndPointID receiver = new I40EndPointID();
            I40Identification seID = new I40Identification();
            I40role seRole = new I40role();

            I40Identification reID = new I40Identification();
            I40role reRole = new I40role();

            seID.id = connectNodeName;
            seID.idType = "idShort";
            seRole.name = senderRole;
            sender.identification = seID;
            sender.role = seRole;
            i40Frame.sender = sender;

            reID.id = receiverId;
            reID.idType = "idShort";
            reRole.name = receiverRole;
            receiver.identification = reID;
            receiver.role = reRole;
            i40Frame.receiver = receiver;

            I40SemanticKey i40Key = new I40SemanticKey();
            i40Key.type = "AasxConnect";
            i40Key.local = "local";
            i40Key.value = "ovgu.de/http://www.vdi.de/gma720/vdi2193_2/bidding";
            i40Key.idType = "False";

            I40SemanticProtocol semanticProtocol = new I40SemanticProtocol();
            semanticProtocol.keys.Add(i40Key);

            i40Frame.semanticProtocol = semanticProtocol;
            i40Frame.messageId = connectNodeName + "1";
            _i40Message.frame = i40Frame;

            return _i40Message;
        }


        public I40Message_Interaction createBiddingMessage(string connectNodeName,
                       string receiverId, string receiverRole, string senderRole, string messageType,
                       string replyBy, string replyTo, string conversationId, int messageCount)
        {
            I40Message_Interaction _i40Message = new I40Message_Interaction();
            I40TransmitFrame i40Frame = new I40TransmitFrame();
            i40Frame.type = messageType;
            i40Frame.replyBy = replyBy;
            i40Frame.replyTo = replyTo;

            I40EndPointID sender = new I40EndPointID();
            I40EndPointID receiver = new I40EndPointID();
            I40Identification seID = new I40Identification();
            I40role seRole = new I40role();

            I40Identification reID = new I40Identification();
            I40role reRole = new I40role();

            seID.id = connectNodeName;
            seID.idType = "idShort";
            seRole.name = senderRole;
            sender.identification = seID;
            sender.role = seRole;
            i40Frame.sender = sender;

            reID.id = receiverId;
            reID.idType = "idShort";
            reRole.name = receiverRole;
            receiver.identification = reID;
            receiver.role = reRole;
            i40Frame.receiver = receiver;

            I40SemanticKey i40Key = new I40SemanticKey();
            i40Key.type = "AasxConnect";
            i40Key.local = "local";
            i40Key.value = "ovgu.de/http://www.vdi.de/gma720/vdi2193_2/bidding";
            i40Key.idType = "False";

            I40SemanticProtocol semanticProtocol = new I40SemanticProtocol();
            semanticProtocol.keys.Add(i40Key);

            i40Frame.semanticProtocol = semanticProtocol;
            i40Frame.conversationId = conversationId;
            i40Frame.messageId = "AASXServerConnect" + messageCount.ToString();
            _i40Message.frame = i40Frame;

            return _i40Message;
        }

    }
}
