using System;
using Game.Enums2;
using NetScaleCommon;

namespace Network
{
    public static class RpcExtensions
    {
        public static void Publish(this ClientToProxy target, RpcTypes type, in DeliveryMethod deliveryMethod)
            => target.Publish((ushort)type, in deliveryMethod);

        public static void Publish<T>(this ClientToProxy target, RpcTypes type, in DeliveryMethod deliveryMethod, in T t1)
            => target.Publish((ushort)type, in deliveryMethod, in t1);

        public static void Publish<T1, T2>(this ClientToProxy target, RpcTypes type, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2)
            => target.Publish((ushort)type, in deliveryMethod, in t1, in t2);

        public static void Publish<T1, T2, T3>(this ClientToProxy target, RpcTypes type, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2, in T3 t3)
            => target.Publish((ushort)type, in deliveryMethod, in t1, in t2, in t3);

        public static void PublishAndForgetExpiring(this ClientToProxy target, in TimeSpan exp, RpcTypes type, in DeliveryMethod deliveryMethod)
            => target.PublishAndForgetExpiring(in exp, (ushort)type, in deliveryMethod);

        public static void PublishAndForgetExpiring<T1>(this ClientToProxy target, in TimeSpan exp, RpcTypes type, in DeliveryMethod deliveryMethod, in T1 t1)
            => target.PublishAndForgetExpiring(in exp, (ushort)type, in deliveryMethod, in t1);

        public static void PublishAndForgetExpiring<T1, T2>(this ClientToProxy target, in TimeSpan exp, RpcTypes type, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2)
            => target.PublishAndForgetExpiring(in exp, (ushort)type, in deliveryMethod, in t1, in t2);

        public static void PublishAndForgetExpiring<T1, T2, T3>(this ClientToProxy target, in TimeSpan exp, RpcTypes type, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2, in T3 t3)
            => target.PublishAndForgetExpiring(in exp, (ushort)type, in deliveryMethod, in t1, in t2, in t3);
    }

}