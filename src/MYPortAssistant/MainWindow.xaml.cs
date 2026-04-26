using System.Text;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace MYPortAssistant;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly List<byte> receiveBuffer = new List<byte>();
    private SerialPort? serialPort;
    private long receiveByteCount;
    private long sendByteCount;

    public MainWindow()
    {
        InitializeComponent();
        InitializeOptions();
        RefreshPorts();
        UpdateConnectionUi(false);
    }

    protected override void OnClosed(EventArgs e)
    {
        CloseSerialPort();
        base.OnClosed(e);
    }

    private void InitializeOptions()
    {
        BaudRateComboBox.ItemsSource = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        BaudRateComboBox.SelectedItem = 115200;

        DataBitsComboBox.ItemsSource = new[] { 5, 6, 7, 8 };
        DataBitsComboBox.SelectedItem = 8;

        StopBitsComboBox.Items.Add(new ComboBoxItem { Content = "1", Tag = StopBits.One });
        StopBitsComboBox.Items.Add(new ComboBoxItem { Content = "1.5", Tag = StopBits.OnePointFive });
        StopBitsComboBox.Items.Add(new ComboBoxItem { Content = "2", Tag = StopBits.Two });
        StopBitsComboBox.SelectedIndex = 0;

        ParityComboBox.Items.Add(new ComboBoxItem { Content = "无", Tag = Parity.None });
        ParityComboBox.Items.Add(new ComboBoxItem { Content = "奇校验", Tag = Parity.Odd });
        ParityComboBox.Items.Add(new ComboBoxItem { Content = "偶校验", Tag = Parity.Even });
        ParityComboBox.Items.Add(new ComboBoxItem { Content = "标记", Tag = Parity.Mark });
        ParityComboBox.Items.Add(new ComboBoxItem { Content = "空格", Tag = Parity.Space });
        ParityComboBox.SelectedIndex = 0;
    }

    private void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPorts();
    }

    private void OpenPortButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSerialPort();
    }

    private void ClosePortButton_Click(object sender, RoutedEventArgs e)
    {
        CloseSerialPort();
        StatusTextBlock.Text = "串口已关闭";
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendData();
    }

    private void ClearReceiveButton_Click(object sender, RoutedEventArgs e)
    {
        receiveBuffer.Clear();
        receiveByteCount = 0;
        ReceiveTextBox.Clear();
        ReceiveInfoTextBlock.Text = "接收字节：0";
        StatusTextBlock.Text = "接收区已清空";
    }

    private void DisplayModeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (ReceiveTextBox == null)
        {
            return;
        }

        RenderReceiveBuffer();
    }

    private void RefreshPorts()
    {
        string? selectedPort = PortNameComboBox.SelectedItem as string;
        string[] ports = SerialPort.GetPortNames().OrderBy(port => port).ToArray();

        PortNameComboBox.ItemsSource = ports;
        PortNameComboBox.SelectedItem = ports.Contains(selectedPort) ? selectedPort : ports.FirstOrDefault();
        StatusTextBlock.Text = ports.Length == 0 ? "未发现串口" : $"发现 {ports.Length} 个串口";
    }

    private void OpenSerialPort()
    {
        if (PortNameComboBox.SelectedItem is not string portName)
        {
            StatusTextBlock.Text = "请先选择串口";
            MessageBox.Show("请先选择串口。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int baudRate = (int)BaudRateComboBox.SelectedItem;
        int dataBits = (int)DataBitsComboBox.SelectedItem;
        StopBits stopBits = GetSelectedTag<StopBits>(StopBitsComboBox);
        Parity parity = GetSelectedTag<Parity>(ParityComboBox);

        try
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.Open();
            UpdateConnectionUi(true);
            StatusTextBlock.Text = $"{portName} 已打开，波特率 {baudRate}";
        }
        catch (UnauthorizedAccessException ex)
        {
            CloseSerialPort();
            ShowSerialError("串口被占用或权限不足", ex.Message);
        }
        catch (IOException ex)
        {
            CloseSerialPort();
            ShowSerialError("串口打开失败", ex.Message);
        }
        catch (ArgumentException ex)
        {
            CloseSerialPort();
            ShowSerialError("串口参数无效", ex.Message);
        }
    }

    private void CloseSerialPort()
    {
        if (serialPort == null)
        {
            UpdateConnectionUi(false);
            return;
        }

        serialPort.DataReceived -= SerialPort_DataReceived;

        if (serialPort.IsOpen)
        {
            serialPort.Close();
        }

        serialPort.Dispose();
        serialPort = null;
        UpdateConnectionUi(false);
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort port = (SerialPort)sender;

        try
        {
            int bytesToRead = port.BytesToRead;
            byte[] bytes = new byte[bytesToRead];
            int readCount = port.Read(bytes, 0, bytes.Length);

            if (readCount != bytes.Length)
            {
                Array.Resize(ref bytes, readCount);
            }

            Dispatcher.Invoke(() =>
            {
                receiveBuffer.AddRange(bytes);
                receiveByteCount += bytes.Length;
                RenderReceiveBuffer();
                ReceiveInfoTextBlock.Text = $"接收字节：{receiveByteCount}";
                StatusTextBlock.Text = $"接收 {bytes.Length} 字节";
            });
        }
        catch (InvalidOperationException ex)
        {
            Dispatcher.Invoke(() => ShowSerialError("串口状态异常", ex.Message));
        }
        catch (IOException ex)
        {
            Dispatcher.Invoke(() => ShowSerialError("串口读取失败", ex.Message));
        }
    }

    private void SendData()
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            StatusTextBlock.Text = "串口未打开";
            return;
        }

        byte[] bytes;

        if (HexSendRadioButton.IsChecked == true)
        {
            if (!TryParseHex(SendTextBox.Text, out bytes, out string errorMessage))
            {
                StatusTextBlock.Text = errorMessage;
                MessageBox.Show(errorMessage, "HEX 输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            bytes = Encoding.ASCII.GetBytes(SendTextBox.Text);
        }

        try
        {
            serialPort.Write(bytes, 0, bytes.Length);
            sendByteCount += bytes.Length;
            SendInfoTextBlock.Text = $"发送字节：{sendByteCount}";
            StatusTextBlock.Text = $"发送 {bytes.Length} 字节";
        }
        catch (InvalidOperationException ex)
        {
            ShowSerialError("串口状态异常", ex.Message);
        }
        catch (TimeoutException ex)
        {
            ShowSerialError("串口发送超时", ex.Message);
        }
    }

    private void RenderReceiveBuffer()
    {
        ReceiveTextBox.Text = HexDisplayRadioButton.IsChecked == true
            ? FormatHex(receiveBuffer)
            : Encoding.ASCII.GetString(receiveBuffer.ToArray());
        ReceiveTextBox.ScrollToEnd();
    }

    private void UpdateConnectionUi(bool isOpen)
    {
        PortNameComboBox.IsEnabled = !isOpen;
        BaudRateComboBox.IsEnabled = !isOpen;
        DataBitsComboBox.IsEnabled = !isOpen;
        StopBitsComboBox.IsEnabled = !isOpen;
        ParityComboBox.IsEnabled = !isOpen;
        RefreshPortsButton.IsEnabled = !isOpen;
        OpenPortButton.IsEnabled = !isOpen;
        ClosePortButton.IsEnabled = isOpen;
        SendButton.IsEnabled = isOpen;
        ConnectionStateTextBlock.Text = isOpen ? "已连接" : "未连接";
        ConnectionStateTextBlock.Foreground = isOpen ? Brushes.ForestGreen : Brushes.Firebrick;
    }

    private static T GetSelectedTag<T>(ComboBox comboBox)
    {
        return (T)((ComboBoxItem)comboBox.SelectedItem).Tag;
    }

    private static string FormatHex(IEnumerable<byte> bytes)
    {
        StringBuilder builder = new();

        foreach (byte value in bytes)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value.ToString("X2"));
        }

        return builder.ToString();
    }

    private static bool TryParseHex(string input, out byte[] bytes, out string errorMessage)
    {
        StringBuilder builder = new();

        foreach (char value in input)
        {
            if (!char.IsWhiteSpace(value))
            {
                builder.Append(value);
            }
        }

        if (builder.Length == 0)
        {
            bytes = new byte[0];
            errorMessage = "HEX 内容不能为空";
            return false;
        }

        if (builder.Length % 2 != 0)
        {
            bytes = new byte[0];
            errorMessage = "HEX 字符数量必须为偶数";
            return false;
        }

        bytes = new byte[builder.Length / 2];

        try
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(builder.ToString(i * 2, 2), 16);
            }
        }
        catch (FormatException)
        {
            bytes = new byte[0];
            errorMessage = "HEX 内容只能包含 0-9、A-F";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void ShowSerialError(string title, string message)
    {
        StatusTextBlock.Text = title;
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}