using Aurora;
using Aurora.EffectsEngine;
using Aurora.Profiles;
using Aurora.Devices;
using Aurora.Utils;
using Aurora.Settings;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;


public class Headset : IEffectScript
{
    public string ID { get; private set; }
    public VariableRegistry Properties { get; private set; }
    public DeviceKeys[] keys = new[] { DeviceKeys.ONE, DeviceKeys.TWO, DeviceKeys.THREE, DeviceKeys.FOUR, DeviceKeys.FIVE, DeviceKeys.SIX, DeviceKeys.SEVEN, DeviceKeys.EIGHT, DeviceKeys.NINE, DeviceKeys.ZERO };
    public KeySequence DefaultKeys;
    private Boolean threadRunning;
    private HeadsetData batteryStatus = new HeadsetData()
    {
        chargeState = "",
        percentage = 0
    };

    private float chargingAnim = 0;
    public Headset()
    {
        DefaultKeys = new KeySequence(this.keys);
        ID = "G933 LGS";
        Properties = new VariableRegistry();
        Properties.Register("keys", DefaultKeys, "Main Keys");
        Properties.Register("foregroundColour", new RealColor(Color.Lime), "Foreground Colour");
        Properties.Register("backgroundColour", new RealColor(Color.Orange), "Background Colour");
        Properties.Register("chargingColour", new RealColor(Color.Cyan), "Charging Foreground Colour");
        Properties.Register("chargingBGColour", new RealColor(Color.Orange), "Charging Background Colour");
        //Can't get this working : allows user to set percent effect type https://github.com/antonpup/Aurora/wiki/T_Aurora_Settings_PercentEffectType
        //Properties.Register("effectType", PercentEffectType.Progressive_Gradual, "Effect Type");
    }

    public void Battery(Object stateInfo)
    {
        String jsonString;
        try
        {
            jsonString = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\..\\Local\\Logitech\\settings.json");
            var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString));
            DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(LgsData));
            LgsData bsObj = (LgsData)deserializer.ReadObject(ms);
            this.batteryStatus = bsObj.headset.data;
            using (StreamWriter writer = new StreamWriter("D:\\out.txt"))
            {
                Console.SetOut(writer);
                Console.Write(bsObj.headset);
            };
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.Write("Error found : LGS settings.json file not found. Try restarting LGS if this errors comes back often." + ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.Error.Write("Error found : LGS folder not found. Maybe LGS is not installed." + ex);
        }
        finally
        {
            Thread.Sleep(2000);
            this.threadRunning = false;
        }
    }
    public object UpdateLights(VariableRegistry settings, IGameState state = null)
    {
        EffectLayer layer = new EffectLayer(this.ID);
        RealColor FG = settings.GetVariable<RealColor>("foregroundColour");
        RealColor BG = settings.GetVariable<RealColor>("backgroundColour");
        if (!this.threadRunning)
        {
            this.threadRunning = true;
            ThreadPool.QueueUserWorkItem(this.Battery);
        }
        if (this.batteryStatus.isCharging == true)
        {
            FG = settings.GetVariable<RealColor>("chargingColour");
            BG = settings.GetVariable<RealColor>("chargingBGColour");
        }
        layer.PercentEffect(FG.GetDrawingColor(), BG.GetDrawingColor(), settings.GetVariable<KeySequence>("keys") ?? DefaultKeys, this.batteryStatus.percentage, 100D, PercentEffectType.Progressive_Gradual);
        return layer;
    }
}

[DataContract]
public class LgsData
{
    [DataMember(Name = "last_battery_status")]
    public HeadsetID headset { get; set; }
}

[DataContract]
public class HeadsetID
{
    [DataMember(Name = "4294967295")]
    public HeadsetData data { get; set; }
}

[DataContract]
public class HeadsetData
{
    [DataMember(Name = "last_state")]
    public String chargeState { get; set; }

    public Boolean isCharging { get { return this.chargeState == "charge"; } }
    [DataMember(Name = "percentage")]
    public float percentage { get; set; }
}