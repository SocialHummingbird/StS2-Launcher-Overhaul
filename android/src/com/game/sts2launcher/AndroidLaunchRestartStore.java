package com.game.sts2launcher;

import org.json.JSONObject;

/** Durable pending -> claimed -> consumed protocol. Claimed requests never replay after process death. */
final class AndroidLaunchRestartStore {
    static final String KEY = "launch_restart_request_v1";
    interface Storage {
        String read();
        boolean write(String value);
    }
    private final Storage storage;
    private String claimed = "";
    private String issuedAttemptId = "";
    AndroidLaunchRestartStore(Storage storage) { this.storage = storage; }

    synchronized String accept(String json, boolean hasTarget, long now) {
        if (!hasTarget) return "Android restart target is unavailable.";
        try {
            JSONObject request = validate(json, now);
            if (!"pending".equals(request.getString("state"))) return "Restart request is not pending.";
            if (!storage.write(request.toString())) return "Android could not durably save the restart request.";
            return "";
        } catch (Exception error) {
            return "Invalid restart request: " + error.getMessage();
        }
    }

    synchronized String acceptAndStart(String json, boolean hasTarget, long now,
        Runnable startTarget) {
        String problem = accept(json, hasTarget, now);
        if (!problem.isEmpty()) return problem;
        try {
            issuedAttemptId = new JSONObject(json).getString("attemptId");
            // Target-start exceptions must reach managed code before it records acceptance.
            startTarget.run();
            return "";
        } catch (Exception error) {
            try {
                storage.write(new JSONObject(json).put("state", "failed")
                    .put("error", "restart-target-failed").toString());
            } catch (Exception ignored) { }
            return "Android could not start the restart target: " + error.getMessage();
        }
    }

    synchronized String finish(String attemptId, Runnable exitProcess) {
        try {
            JSONObject request = new JSONObject(storage.read());
            if (attemptId == null || attemptId.isEmpty() || !attemptId.equals(issuedAttemptId)
                || !attemptId.equals(request.getString("attemptId"))
                || !"pending".equals(request.getString("state")))
                return "Android restart completion does not match an accepted pending request.";
            exitProcess.run();
            return "";
        } catch (Exception error) {
            return "Android could not complete the process restart: " + error.getMessage();
        }
    }

    synchronized String claim(String branch, long now) {
        if (!claimed.isEmpty()) return claimed;
        try {
            JSONObject request = validate(storage.read(), now);
            // The store is process-static in GodotApp, including Activity recreation.
            if (request.getString("attemptId").equals(issuedAttemptId)) return "";
            if (!"pending".equals(request.getString("state")) || !branch.equals(request.getString("branch"))) return "";
            request.put("state", "claimed");
            String next = request.toString();
            if (!storage.write(next)) return "";
            claimed = next;
            return claimed;
        } catch (Exception error) { return ""; }
    }

    synchronized String consume() {
        if (claimed.isEmpty()) return "";
        try {
            if (!claimed.equals(storage.read())) { claimed = ""; return ""; }
            JSONObject request = new JSONObject(claimed);
            request.put("state", "consumed");
            if (!storage.write(request.toString())) return "";
            String result = claimed;
            claimed = "";
            return result;
        } catch (Exception error) { return ""; }
    }

    static JSONObject validate(String json, long now) throws Exception {
        JSONObject request = new JSONObject(json);
        if (request.getInt("version") != 1) throw new IllegalArgumentException("Unsupported request version.");
        for (String key : new String[] { "attemptId", "branch", "state" }) {
            if (request.getString(key).trim().isEmpty()) throw new IllegalArgumentException("Missing " + key);
        }
        if (!request.get("safe").getClass().equals(Boolean.class)) throw new IllegalArgumentException("Invalid safe mode.");
        if (!request.optBoolean("legacy", false)) {
            for (String key : new String[] { "gameIdentityId", "runtimePackId", "generation" }) {
                if (request.getString(key).trim().isEmpty()) throw new IllegalArgumentException("Missing " + key);
            }
        }
        long created = request.getLong("createdAtUnixMs");
        if (created <= 0 || created > now + 60000 || now - created > 86400000)
            throw new IllegalArgumentException("Restart request is stale.");
        return request;
    }
}
