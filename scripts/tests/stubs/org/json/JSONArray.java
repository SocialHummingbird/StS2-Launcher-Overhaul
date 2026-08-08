package org.json;

import java.util.ArrayList;
import java.util.List;

public final class JSONArray {
	private final List<Object> values;

	public JSONArray(String text) {
		this(asArray(JsonParser.parse(text)));
	}

	JSONArray(List<Object> values) {
		this.values = new ArrayList<>(values);
	}

	public int length() {
		return values.size();
	}

	public String optString(int index, String fallback) {
		if (index < 0 || index >= values.size()) {
			return fallback;
		}
		Object value = values.get(index);
		return value instanceof String ? (String) value : fallback;
	}

	@SuppressWarnings("unchecked")
	private static List<Object> asArray(Object value) {
		if (!(value instanceof List<?>)) {
			throw new IllegalArgumentException("JSON value is not an array");
		}
		return (List<Object>) value;
	}
}
