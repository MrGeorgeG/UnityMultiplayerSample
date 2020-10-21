using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using UnityEditor;
using System.Collections;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;
    public GameObject playerCube;
    public GameObject CubeObj;
    String ID;
    //

    void Start()
    {

        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        ID = UnityEngine.Random.value.ToString();
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        PlayerIntoMsg p = new PlayerIntoMsg();
        GameObject playerIfmiont = Instantiate(playerCube, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
        playerIfmiont.AddComponent<Playermovement>();
        p.Point = playerIfmiont.transform.position;
        p.UnityID = ID;
        CubeObj = playerIfmiont;
        StartCoroutine(SendPos());
        // Example to send a handshake message:
        SendToServer(JsonUtility.ToJson(p));
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                Debug.Log("Handshake message received!");
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                Debug.Log("Player update message received!");
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                Debug.Log("Server update message received!");
                break;
            case Commands.PLAYER_INTO:
                PlayerIntoMsg pMsg = JsonUtility.FromJson<PlayerIntoMsg>(recMsg);
                Debug.Log("Player into message received!");
                if (ID != pMsg.UnityID)
                {
                    Instantiate(playerCube, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);

                }
                break;
            case Commands.PLAYER_CUBE:
                PlayerCubeMsg pcMsg = JsonUtility.FromJson<PlayerCubeMsg>(recMsg);
                Debug.Log("Player movement update message received!");
                foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (p != CubeObj && pcMsg.UnityID != ID)
                    {
                        p.transform.position = pcMsg.Point;
                    }

                }

                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect()
    {
        //Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }
    IEnumerator SendPos()
    {
        while(true)
        {
            
            if (CubeObj)
            {
                yield return new WaitForSeconds(0.1f);
                PlayerCubeMsg m = new PlayerCubeMsg();
                m.Point = CubeObj.transform.position;
                m.UnityID = ID;
                SendToServer(JsonUtility.ToJson(m));
                Debug.Log("SPtext");

            }
        }
    }

    void Update()
    {

        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}