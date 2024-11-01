using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotNetBlueZ;
using DotNetBlueZ.Extensions;
using PeterO.Cbor;
using SmpClient.ImageUtils;
using SmpClient.Message;
using SmpClient.Responses;

namespace SmpClient;

public static class SmpClientManager
{
    private static int MtuSize = 20;
    private static int BufferSize = 0;
    private static byte[] buffer = [];
    private static TaskCompletionSource<bool> notifyTcs = new();
    private static GattCharacteristic? characteristic = null!;

    public static async Task StartDfu(Adapter adapter, string address, string smpServiceUuid, string smpCharacteristicuuid, string filePath)
    {
        Device? device = null;
        try
        {
            device = await adapter.GetDeviceAsync(address);
            await device.ConnectAsync().TimeoutAfter(10000);
            await device.GetServicesResolvedAsync();
            await Task.Delay(1500);
            var service = await device.GetServiceAsync(smpServiceUuid);
            characteristic = await service.GetCharacteristicAsync(smpCharacteristicuuid);
            var (fd, mtu) = await characteristic.AcquireWriteAsync(new Dictionary<string, object>());
            MtuSize = mtu - 3;
            fd.Close();

            await characteristic.StartNotifyAsync();
            characteristic.Value += NotifyCallback;

            if (await IsImageExist(filePath, device)) return;

            var response = await SendAndReceive(new McumgrParametersRead().SmpData);
            var mcumgrParameters = CBORObject.DecodeObjectFromBytes<McumgrParametersReadResponse>(response[8..]);
            if (HeaderUtils.UnpackVersion(response[0]) == Version.V1)
            {
                BufferSize = MtuSize;
            }
            else
            {
                BufferSize = mcumgrParameters.Buf_size;
            }
            byte[] image = await File.ReadAllBytesAsync(filePath);
            var sha = SHA256.HashData(image);
            response = await SendAndReceive(MaximizeImageUploadWritePacket(new(0, [], 0, image.Length, sha, false), image).SmpData);
            var imageUploadWriteResponse = CBORObject.DecodeObjectFromBytes<ImageUploadWriteResponse>(response[8..]);
            while (imageUploadWriteResponse.Off != image.Length)
            {
                response = await SendAndReceive(MaximizeImageUploadWritePacket(new(imageUploadWriteResponse.Off, []), image).SmpData);
                imageUploadWriteResponse = CBORObject.DecodeObjectFromBytes<ImageUploadWriteResponse>(response[8..]);
            }

            response = await SendAndReceive(new ImageStatesRead().SmpData);
            var imageStatesReadResponse = CBORObject.DecodeObjectFromBytes<ImageStatesReadResponse>([.. response[8..]]);
            await SendAndReceive(new ImageStatesWrite(imageStatesReadResponse.Images[1].Hash, true).SmpData);
            await SendAndReceive(new ResetWrite().SmpData);
            return;
        }
        catch (Exception)
        {
            await device.DisconnectAsync();
            throw;
        }
        finally
        {
            characteristic.Value -= NotifyCallback;
        }
    }

    private static async Task NotifyCallback(GattCharacteristic sender, GattCharacteristicValueEventArgs eventArgs)
    {
        buffer = [.. buffer, .. eventArgs.Value];
        notifyTcs.TrySetResult(true);
    }

    private static async Task<bool> IsImageExist(string filePath, Device device)
    {
        var imageSha = GetHashValueFromFirmware(filePath);
        var response = await SendAndReceive(new ImageStatesRead().SmpData);
        var imageStatesReadResponse = CBORObject.DecodeObjectFromBytes<ImageStatesReadResponse>([.. response[8..]]);

        if ((imageStatesReadResponse.Images.Count == 1 || imageStatesReadResponse.Images.Count == 2) && imageSha.SequenceEqual(imageStatesReadResponse.Images[0].Hash))
        {
            await device.DisconnectAsync();
            return true;
        }
        if (imageStatesReadResponse.Images.Count == 2 && imageSha.SequenceEqual(imageStatesReadResponse.Images[1].Hash))
        {
            await SendAndReceive(new ImageStatesWrite(imageStatesReadResponse.Images[1].Hash, true).SmpData);
            await SendAndReceive(new ResetWrite().SmpData);
            return true;
        }

        return false;
    }

    private static async Task<byte[]> Receive()
    {
        notifyTcs = new TaskCompletionSource<bool>();
        await notifyTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(5));

        if (buffer.Length < 8)
        {
            throw new Exception("Buffer contents not big enough for SMP header.");
        }
        var header = buffer[..8];
        var messageLength = header.Length + (ushort)(header[2] << 8 | header[3]);
        while (buffer.Length < messageLength)
        {
            notifyTcs = new TaskCompletionSource<bool>();
            await notifyTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        }

        var message = buffer;
        buffer = [];
        return message;
    }

    private static async Task Send(byte[] data)
    {
        for (int offset = 0; offset < data.Length; offset += MtuSize)
        {
            var chunk = new byte[Math.Min(MtuSize, data.Length - offset)];
            Array.Copy(data, offset, chunk, 0, chunk.Length);
            await characteristic.WriteValueAsync(chunk, new Dictionary<string, object>());
        }
    }

    private static async Task<byte[]> SendAndReceive(byte[] data)
    {
        await Send(data);
        return await Receive();
    }

    private static byte[] GetHashValueFromFirmware(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var header = ImageHeader.FromStream(stream);
        stream.Seek(header.HdrSize + header.ImgSize, SeekOrigin.Begin);
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);
        ushort tlvMagic = reader.ReadUInt16();
        ushort tlvTotal = reader.ReadUInt16();
        ImageTLV imageTLV = null!;
        while (stream.Position < header.HdrSize + header.ImgSize + tlvTotal)
        {
            imageTLV = ImageTLV.FromStream(reader);
        }
        return imageTLV.Value[1..33];
    }

    private static async Task TimeoutAfter(this Task task, int timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("The operation has timed out.");
        }

        await task;
    }

    private static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Notification was not recieved in 2 seconds");
        }

        return await task;
    }

    private static int GetCborIntegerSize(int integer)
    {
        return integer < 24 ? 0 :
               integer < 0xFF ? 1 :
               integer < 0xFFFF ? 2 : 4;
    }

    private static Tuple<int, int> GetMaxCborAndDataSize(ImageUploadWrite request)
    {
        int unencodedBytesAvailable = BufferSize - request.SmpData.Length;
        int bytesRequiredToEncodeDataSize = GetCborIntegerSize(unencodedBytesAvailable);
        int dataSize = Math.Max(0, unencodedBytesAvailable - bytesRequiredToEncodeDataSize);
        int cborSize = request.Header.Length + dataSize + GetCborIntegerSize(dataSize);
        return Tuple.Create(cborSize, dataSize);
    }

    private static ImageUploadWrite MaximizeImageUploadWritePacket(ImageUploadWrite request, byte[] image)
    {
        Header h = request.Header;
        var (cborSize, dataSize) = GetMaxCborAndDataSize(request);
        if (dataSize > image.Length - request.Off)
        {
            dataSize = image.Length - request.Off;
            cborSize = h.Length + dataSize + GetCborIntegerSize(dataSize);
        }
        byte[] data = new byte[dataSize];
        Array.Copy(image, request.Off, data, 0, dataSize);

        return new ImageUploadWrite(
            off: request.Off,
            data: data,
            image: request.Image,
            length: request.Length,
            sha: request.Sha,
            upgrade: request.Upgrade,
            sequence: request.Header.Sequence,
            header: new Header(
                op: h.Op,
                version: h.Version,
                flags: h.Flags,
                length: (ushort)cborSize,
                groupId: h.GroupId,
                commandId: h.CommandId,
                sequence: h.Sequence

            )
        );
    }
}