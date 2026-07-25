using CritterCab.Telemetry.V1;
using Wolverine;

namespace CritterCab.Telemetry.ReportLocations;

// W006 §6.3 — the real slice-3 publish. CritterCab's first Kafka producer.
//
// Almost nothing happens here, and that is the point: the topic, serializer, partition-key
// source, and durability mode are all declared once as routing configuration in Program.cs.
// Kafka is a transport wire, not a handler shape — so this class holds no topic name, no
// offsets, and no broker types, and ReportLocationsHandler (which calls it) holds even less.
// Swapping LoggingDriverLocationPublisher for this one is the entire behavioral change of PR C.
//
// Registered SCOPED, unlike the singleton logging stub it replaces: IMessageBus is itself
// registered scoped by Wolverine (HostBuilderExtensions.cs:232), so a singleton holding one
// would be a captive dependency. The ingest handler is resolved per window, so a scoped
// publisher lives exactly as long as the window that uses it.
public sealed class KafkaDriverLocationPublisher(IMessageBus bus) : IDriverLocationPublisher
{
    public Task PublishAsync(DriverLocationUpdated update, CancellationToken ct)
    {
        // PartitionKey is what makes per-driver ordering a property of the transport rather
        // than something Dispatch has to reconstruct (R7): every record for a driver lands on
        // one partition and is consumed in order. Wolverine falls back to the envelope's GUID
        // when this is unset, which would scatter a driver's positions across partitions and
        // silently destroy the ordering guarantee — the failure is invisible until a consumer
        // computes heading or speed from out-of-order fixes.
        //
        // The key is the envelope's PartitionKey, NOT the envelope's GroupId. Those two swap
        // roles under PublishRawJson(), whose mapper assigns Key = GroupId after the transport
        // has already set it from PartitionKey. This topic publishes binary protobuf, so that
        // path is not in play — but never combine the two.
        //
        // ct has nowhere to go: IMessageBus.PublishAsync takes no CancellationToken. Kept on
        // the seam anyway, because the seam is the contract and a future implementation (or a
        // consumer-side counterpart) may have somewhere to put it.
        return bus.PublishAsync(update, new DeliveryOptions { PartitionKey = update.DriverId })
            .AsTask();
    }
}
