namespace CritterCab.Dispatch.FareQuoting;

// Marker for the two terminal outcomes of a fare quote attempt — emitted by
// FareQuoteAutomation per W001 §5.2. Wolverine uses the runtime type when
// appending the returned event to the stream; the marker lets the handler's
// return type document the domain choice.
public interface IFareQuoteOutcome;
