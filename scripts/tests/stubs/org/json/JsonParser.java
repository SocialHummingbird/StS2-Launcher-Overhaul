package org.json;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

final class JsonParser {
	private final String text;
	private int index;

	private JsonParser(String text) {
		this.text = text == null ? "" : text;
	}

	static Object parse(String text) {
		JsonParser parser = new JsonParser(text);
		Object value = parser.readValue();
		parser.skipWhitespace();
		if (parser.index != parser.text.length()) {
			throw parser.error("Unexpected trailing content");
		}
		return value;
	}

	private Object readValue() {
		skipWhitespace();
		if (index >= text.length()) {
			throw error("Expected a JSON value");
		}
		char current = text.charAt(index);
		switch (current) {
			case '{':
				return readObject();
			case '[':
				return readArray();
			case '"':
				return readString();
			case 't':
				readLiteral("true");
				return Boolean.TRUE;
			case 'f':
				readLiteral("false");
				return Boolean.FALSE;
			case 'n':
				readLiteral("null");
				return null;
			default:
				return readNumber();
		}
	}

	private Map<String, Object> readObject() {
		LinkedHashMap<String, Object> values = new LinkedHashMap<>();
		expect('{');
		skipWhitespace();
		if (consume('}')) {
			return values;
		}
		while (true) {
			skipWhitespace();
			if (index >= text.length() || text.charAt(index) != '"') {
				throw error("Expected an object key");
			}
			String key = readString();
			skipWhitespace();
			expect(':');
			values.put(key, readValue());
			skipWhitespace();
			if (consume('}')) {
				return values;
			}
			expect(',');
		}
	}

	private List<Object> readArray() {
		ArrayList<Object> values = new ArrayList<>();
		expect('[');
		skipWhitespace();
		if (consume(']')) {
			return values;
		}
		while (true) {
			values.add(readValue());
			skipWhitespace();
			if (consume(']')) {
				return values;
			}
			expect(',');
		}
	}

	private String readString() {
		expect('"');
		StringBuilder value = new StringBuilder();
		while (index < text.length()) {
			char current = text.charAt(index++);
			if (current == '"') {
				return value.toString();
			}
			if (current != '\\') {
				value.append(current);
				continue;
			}
			if (index >= text.length()) {
				throw error("Unterminated string escape");
			}
			char escaped = text.charAt(index++);
			switch (escaped) {
				case '"':
				case '\\':
				case '/':
					value.append(escaped);
					break;
				case 'b':
					value.append('\b');
					break;
				case 'f':
					value.append('\f');
					break;
				case 'n':
					value.append('\n');
					break;
				case 'r':
					value.append('\r');
					break;
				case 't':
					value.append('\t');
					break;
				case 'u':
					value.append(readUnicodeEscape());
					break;
				default:
					throw error("Unsupported string escape");
			}
		}
		throw error("Unterminated string");
	}

	private char readUnicodeEscape() {
		if (index + 4 > text.length()) {
			throw error("Incomplete Unicode escape");
		}
		String digits = text.substring(index, index + 4);
		index += 4;
		try {
			return (char) Integer.parseInt(digits, 16);
		} catch (NumberFormatException error) {
			throw error("Invalid Unicode escape");
		}
	}

	private Number readNumber() {
		int start = index;
		if (consume('-')) {
			if (index >= text.length()) {
				throw error("Invalid number");
			}
		}
		while (index < text.length() && Character.isDigit(text.charAt(index))) {
			index++;
		}
		if (consume('.')) {
			while (
				index < text.length()
					&& Character.isDigit(text.charAt(index))
			) {
				index++;
			}
		}
		if (
			index < text.length()
				&& (
					text.charAt(index) == 'e'
						|| text.charAt(index) == 'E'
				)
		) {
			index++;
			if (
				index < text.length()
					&& (
						text.charAt(index) == '+'
							|| text.charAt(index) == '-'
					)
			) {
				index++;
			}
			while (
				index < text.length()
					&& Character.isDigit(text.charAt(index))
			) {
				index++;
			}
		}
		if (start == index) {
			throw error("Invalid JSON value");
		}
		String number = text.substring(start, index);
		try {
			return number.contains(".")
					|| number.contains("e")
					|| number.contains("E")
				? Double.parseDouble(number)
				: Long.parseLong(number);
		} catch (NumberFormatException error) {
			throw error("Invalid number");
		}
	}

	private void readLiteral(String literal) {
		if (!text.regionMatches(index, literal, 0, literal.length())) {
			throw error("Invalid literal");
		}
		index += literal.length();
	}

	private void expect(char expected) {
		skipWhitespace();
		if (!consume(expected)) {
			throw error("Expected '" + expected + "'");
		}
	}

	private boolean consume(char expected) {
		if (index < text.length() && text.charAt(index) == expected) {
			index++;
			return true;
		}
		return false;
	}

	private void skipWhitespace() {
		while (
			index < text.length()
				&& Character.isWhitespace(text.charAt(index))
		) {
			index++;
		}
	}

	private IllegalArgumentException error(String message) {
		return new IllegalArgumentException(message + " at index " + index);
	}
}
