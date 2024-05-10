using HslCommunication.Core.Address;
using HslCommunication.ModBus;
using HslCommunication;
using System;
using System.Text;
using HslCommunication.Core;
using HslCommunication.MQTT;
using System.Linq;
using System.Threading;

class Program
{
    static void Main()
    {
        byte deviceAddress = 0x02; // 设备地址
        ushort registerAddress = 0x000D; // 寄存器地址
        ushort length = 0x0001; // 数据长度
        OperateResult<byte[]> command = ModbusMqtt.BuildReadModbusCommandBytes(registerAddress.ToString(), length, deviceAddress, true, ModbusInfo.ReadRegister, true);
        string hexString = BitConverter.ToString(command.Content).Replace("-", " ");
        Console.WriteLine("MQTT发送ModbusRtu读取指令：" + hexString);
        byte[] byteArray = hexString.Split(' ')
                              .Select(s => Convert.ToByte(s, 16))
                              .ToArray();
        MqttConnectionOptions options = new MqttConnectionOptions
        {
            CleanSession = true,
            IpAddress = "broker.emqx.io",
            Port = 1883,
            ClientId = "MqttTest_1"
        };


        MqttClient mqttClient = new MqttClient(options);
        mqttClient.ConnectServer();
        if (mqttClient.SubscribeMessage("sub").IsSuccess == true && mqttClient.SubscribeMessage("pub").IsSuccess == true)
        {
            Thread.Sleep(1000);
            if (mqttClient.PublishMessage(new MqttApplicationMessage()
            {
                Topic = "sub",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                Payload = byteArray,
                Retain = false
            }).IsSuccess)
            {
                Console.WriteLine("连接MQTT客户端成功...");
                Console.WriteLine("订阅sub、pub主题成功...");
            }
            Thread.Sleep(1000);
            //订阅MQTT数据接收事件，添加对应事件处理函数
            mqttClient.OnMqttMessageReceived += onMqttMessageReceived;
            Thread.Sleep(Timeout.Infinite);

        }
        mqttClient.ConnectClose();
    }

    private static void onMqttMessageReceived(MqttClient client, string topic, byte[] payload)
    {
        if (topic == "pub")
        {
            // 解析收到的消息
            byte[] receivedData = payload;
            string hexString = BitConverter.ToString(receivedData).Replace("-", "");
            Console.WriteLine("MQTT接收ModbusRtu返回指令：" + hexString);
            short value = ModbusMqtt.ReadShortValue(receivedData);
            Console.WriteLine("点位Short值：" + value);
        }
    }
}
