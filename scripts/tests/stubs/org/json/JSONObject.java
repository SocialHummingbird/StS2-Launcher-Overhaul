package org.json;

import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public final class JSONObject {
	private final Map<String, Object> values;

	public JSONObject(String text) {
		this(asObject(JsonParser.parse(text)));
	}

	JSONObject(Map<String, Object> values) {
		this.values = new LinkedHashMap<>(values);
	}

	public int length() {
		return values.size();
	}

	public Iterator<String> keys() {
		return values.keySet().iterator();
	}

	public String optString(String key, String fallback) {
		Object value = values.get(key);
		return value instanceof String ? (String) value : fallback;
	}

	public boolean optBoolean(String key, boolean fallback) {
		Object value = values.get(key);
		return value instanceof Boolean ? (Boolean) value : fallback;
	}

	@SuppressWarnings("unchecked")
	public JSONArray optJSONArray(String key) {
		Object value = values.get(key);
		return value instanceof List<?>
			? new JSONArray((List<Object>) value)
			: null;
	}

	@SuppressWarnings("unchecked")
	public JSONObject optJSONObject(String key) {
		Object value = values.get(key);
		return value instanceof Map<?, ?>
			? new JSONObject((Map<String, Object>) value)
			: null;
	}

	@SuppressWarnings("unchecked")
	private static Map<String, Object> asObject(Object value) {
		if (!(value instanceof Map<?, ?>)) {
			throw new IllegalArgumentException("JSON value is not an object");
		}
		return (Map<String, Object>) value;
	}
}
