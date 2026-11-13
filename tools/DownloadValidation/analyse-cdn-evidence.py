"""Summarise exported, sanitised request events without opening private backups."""
import json
import sys
from collections import defaultdict
from pathlib import Path

root = Path(sys.argv[1])
java = defaultdict(list)
for line in (root / "java-http-events.jsonl").read_text(encoding="utf-8-sig").splitlines():
    event = json.loads(line)
    java[(event.get("processId"), event["requestId"])].append(event)
managed = defaultdict(list)
for line in (root / "http-events.jsonl").read_text(encoding="utf-8-sig").splitlines():
    event = json.loads(line)["entry"]
    managed[event["RequestId"]].append(event)

requests = []
for (pid, request_id), events in java.items():
    first, last = events[0], events[-1]
    if first["category"] not in ("manifest", "chunk"):
        continue
    phases = {event["phase"]: event["elapsedMs"] for event in events}
    requests.append({
        "processId": pid, "requestId": request_id, "kind": first["category"],
        "scheme": first["scheme"], "host": first["host"], "port": first["port"],
        "identity": first.get("contentIdentity"), "status": last["status"],
        "bodyBytes": last["receivedBodyBytes"], "javaPhaseElapsedMs": phases,
        "managedPhaseElapsedMs": {event["Phase"]: event["ElapsedMs"] for event in managed[request_id]},
        "managedCallDurationsMs": [event["DispatchOrPollMs"] for event in managed[request_id]],
        "exceptionChains": [event["exceptionChain"] for event in events if "exceptionChain" in event],
    })
print(json.dumps({
    "clockWarning": "Compare elapsed values within their own clock domain; phase-zero instants differ.",
    "peakJavaWorkers": max(event["activeJavaWorkers"] for events in java.values() for event in events),
    "managedCancellationEvents": sum(event["Phase"] == "cancelled" for events in managed.values() for event in events),
    "requests": requests,
}, indent=2))
