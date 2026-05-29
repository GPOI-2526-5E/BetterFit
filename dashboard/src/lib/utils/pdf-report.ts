export type PdfReportStat = {
	label: string;
	value: string;
	note?: string;
};

export type PdfReportColumn = {
	header: string;
	width?: number;
};

export type PdfReportTable = {
	title: string;
	columns: Array<string | PdfReportColumn>;
	rows: string[][];
	emptyText?: string;
};

export type PdfReportDocument = {
	title: string;
	subtitle?: string;
	reference?: string;
	scope?: string;
	generatedAt?: string;
	stats?: PdfReportStat[];
	tables: PdfReportTable[];
	orientation?: 'portrait' | 'landscape';
};

const PORTRAIT = { width: 595.28, height: 841.89 };
const LANDSCAPE = { width: 841.89, height: 595.28 };
const MARGIN = 38;
const FONT_REGULAR = 'F1';
const FONT_BOLD = 'F2';

const winAnsiSpecialBytes: Record<number, number> = {
	0x20ac: 0x80,
	0x201a: 0x82,
	0x0192: 0x83,
	0x201e: 0x84,
	0x2026: 0x85,
	0x2020: 0x86,
	0x2021: 0x87,
	0x02c6: 0x88,
	0x2030: 0x89,
	0x0160: 0x8a,
	0x2039: 0x8b,
	0x0152: 0x8c,
	0x017d: 0x8e,
	0x2018: 0x91,
	0x2019: 0x92,
	0x201c: 0x93,
	0x201d: 0x94,
	0x2022: 0x95,
	0x2013: 0x96,
	0x2014: 0x97,
	0x02dc: 0x98,
	0x2122: 0x99,
	0x0161: 0x9a,
	0x203a: 0x9b,
	0x0153: 0x9c,
	0x017e: 0x9e,
	0x0178: 0x9f
};

function normalizeText(value: string | number | null | undefined) {
	return String(value ?? '')
		.replace(/\s+/g, ' ')
		.trim();
}

function winAnsiByte(char: string) {
	const code = char.codePointAt(0) ?? 63;
	if (code <= 0x7f || (code >= 0xa0 && code <= 0xff)) {
		return code;
	}

	return winAnsiSpecialBytes[code] ?? 63;
}

function pdfString(value: string) {
	let output = '(';
	for (const char of normalizeText(value)) {
		const byte = winAnsiByte(char);
		if (byte === 40 || byte === 41 || byte === 92) {
			output += `\\${String.fromCharCode(byte)}`;
		} else if (byte < 32 || byte > 126) {
			output += `\\${byte.toString(8).padStart(3, '0')}`;
		} else {
			output += String.fromCharCode(byte);
		}
	}

	return `${output})`;
}

function hexToRgb(color: string) {
	const value = color.replace('#', '');
	const full =
		value.length === 3
			? value
					.split('')
					.map((item) => item + item)
					.join('')
			: value;
	return {
		r: parseInt(full.slice(0, 2), 16) / 255,
		g: parseInt(full.slice(2, 4), 16) / 255,
		b: parseInt(full.slice(4, 6), 16) / 255
	};
}

function colorCommand(color: string, mode: 'fill' | 'stroke') {
	const { r, g, b } = hexToRgb(color);
	return `${r.toFixed(3)} ${g.toFixed(3)} ${b.toFixed(3)} ${mode === 'fill' ? 'rg' : 'RG'}`;
}

function estimateWidth(text: string, fontSize: number) {
	return normalizeText(text).length * fontSize * 0.48;
}

function wrapText(text: string, maxWidth: number, fontSize: number, maxLines = 4) {
	const words = normalizeText(text).split(' ').filter(Boolean);
	const lines: string[] = [];
	let line = '';

	for (const word of words) {
		const candidate = line ? `${line} ${word}` : word;
		if (estimateWidth(candidate, fontSize) <= maxWidth) {
			line = candidate;
			continue;
		}

		if (line) {
			lines.push(line);
			line = word;
		} else {
			let chunk = '';
			for (const char of word) {
				const next = `${chunk}${char}`;
				if (estimateWidth(next, fontSize) > maxWidth && chunk) {
					lines.push(chunk);
					chunk = char;
				} else {
					chunk = next;
				}
			}
			line = chunk;
		}

		if (lines.length >= maxLines) {
			break;
		}
	}

	if (line && lines.length < maxLines) {
		lines.push(line);
	}

	if (lines.length === maxLines && words.join(' ') !== lines.join(' ')) {
		lines[maxLines - 1] =
			`${lines[maxLines - 1].slice(0, Math.max(1, lines[maxLines - 1].length - 1))}...`;
	}

	return lines.length > 0 ? lines : ['-'];
}

function normalizeFileName(value: string) {
	return (
		value
			.toLowerCase()
			.replace(/[^a-z0-9-_]+/gi, '-')
			.replace(/-+/g, '-')
			.replace(/^-|-$/g, '') || 'betterfit-report'
	);
}

export function buildPdfReport(report: PdfReportDocument) {
	const size = report.orientation === 'landscape' ? LANDSCAPE : PORTRAIT;
	const pageWidth = size.width;
	const pageHeight = size.height;
	const innerWidth = pageWidth - MARGIN * 2;
	const pages: string[] = [];
	let commands = '';
	let y = MARGIN;

	const pdfY = (top: number, height = 0) => pageHeight - top - height;

	function addPage() {
		if (commands) {
			pages.push(commands);
		}
		commands = '';
		y = MARGIN;
	}

	function ensureSpace(height: number) {
		if (y + height > pageHeight - MARGIN) {
			addPage();
		}
	}

	function rect(x: number, top: number, width: number, height: number, color: string) {
		commands += `${colorCommand(color, 'fill')} ${x.toFixed(2)} ${pdfY(top, height).toFixed(2)} ${width.toFixed(2)} ${height.toFixed(2)} re f\n`;
	}

	function strokeRect(x: number, top: number, width: number, height: number, color: string) {
		commands += `${colorCommand(color, 'stroke')} ${x.toFixed(2)} ${pdfY(top, height).toFixed(2)} ${width.toFixed(2)} ${height.toFixed(2)} re S\n`;
	}

	function text(
		value: string,
		x: number,
		top: number,
		options: { size?: number; bold?: boolean; color?: string } = {}
	) {
		const fontSize = options.size ?? 10;
		const font = options.bold ? FONT_BOLD : FONT_REGULAR;
		commands += `${colorCommand(options.color ?? '#0f172a', 'fill')} BT /${font} ${fontSize.toFixed(2)} Tf ${x.toFixed(2)} ${pdfY(top + fontSize).toFixed(2)} Td ${pdfString(value)} Tj ET\n`;
	}

	function paragraph(
		value: string,
		x: number,
		top: number,
		width: number,
		fontSize = 10,
		color = '#475569'
	) {
		const lines = wrapText(value, width, fontSize, 8);
		lines.forEach((line, index) =>
			text(line, x, top + index * (fontSize + 4), { size: fontSize, color })
		);
		return lines.length * (fontSize + 4);
	}

	function drawHero() {
		const heroHeight = 82;
		rect(MARGIN, y, innerWidth, heroHeight, '#1769ff');
		text('BETTERFIT REPORT', MARGIN + 18, y + 14, { size: 9, bold: true, color: '#ffffff' });
		text(report.title, MARGIN + 18, y + 31, { size: 22, bold: true, color: '#ffffff' });
		if (report.subtitle) {
			paragraph(report.subtitle, MARGIN + 18, y + 58, innerWidth * 0.68, 9, '#ffffff');
		}
		if (report.reference) {
			text('Riferimento', pageWidth - MARGIN - 118, y + 22, {
				size: 8,
				bold: true,
				color: '#dbeafe'
			});
			text(report.reference, pageWidth - MARGIN - 118, y + 39, {
				size: 12,
				bold: true,
				color: '#ffffff'
			});
		}
		y += heroHeight + 18;
	}

	function drawMeta() {
		const items = [
			report.scope ? ['Perimetro', report.scope] : null,
			report.generatedAt ? ['Generato', report.generatedAt] : null
		].filter(Boolean) as string[][];

		if (items.length === 0) {
			return;
		}

		ensureSpace(42);
		const boxHeight = 38;
		rect(MARGIN, y, innerWidth, boxHeight, '#f8fafc');
		strokeRect(MARGIN, y, innerWidth, boxHeight, '#dbe4f0');
		const columnWidth = innerWidth / items.length;
		items.forEach(([label, value], index) => {
			const x = MARGIN + index * columnWidth + 14;
			text(label, x, y + 9, { size: 7, bold: true, color: '#64748b' });
			text(value, x, y + 22, { size: 10, bold: true });
		});
		y += boxHeight + 16;
	}

	function drawStats(stats: PdfReportStat[]) {
		if (stats.length === 0) {
			return;
		}

		const columns = report.orientation === 'landscape' ? 4 : 3;
		const gap = 10;
		const cardWidth = (innerWidth - gap * (columns - 1)) / columns;
		const cardHeight = 58;

		for (let index = 0; index < stats.length; index += columns) {
			ensureSpace(cardHeight + 12);
			stats.slice(index, index + columns).forEach((stat, columnIndex) => {
				const x = MARGIN + columnIndex * (cardWidth + gap);
				rect(x, y, cardWidth, cardHeight, '#f8fafc');
				strokeRect(x, y, cardWidth, cardHeight, '#dbe4f0');
				text(stat.label, x + 11, y + 10, { size: 7, bold: true, color: '#64748b' });
				text(stat.value, x + 11, y + 25, { size: 16, bold: true });
				if (stat.note) {
					text(stat.note, x + 11, y + 45, { size: 8, color: '#475569' });
				}
			});
			y += cardHeight + 12;
		}
		y += 4;
	}

	function drawTable(table: PdfReportTable) {
		const columns = table.columns.map((column) =>
			typeof column === 'string' ? { header: column, width: 1 } : { width: 1, ...column }
		);
		const totalWeight = columns.reduce((total, column) => total + (column.width ?? 1), 0);
		const widths = columns.map((column) => ((column.width ?? 1) / totalWeight) * innerWidth);
		const headerHeight = 24;
		const fontSize = columns.length > 6 ? 7.5 : 8.5;

		ensureSpace(62);
		text(table.title, MARGIN, y, { size: 14, bold: true });
		y += 22;

		if (table.rows.length === 0) {
			rect(MARGIN, y, innerWidth, 34, '#f8fafc');
			strokeRect(MARGIN, y, innerWidth, 34, '#dbe4f0');
			text(table.emptyText ?? 'Nessun dato disponibile.', MARGIN + 12, y + 12, {
				size: 9,
				color: '#475569'
			});
			y += 48;
			return;
		}

		ensureSpace(headerHeight + 26);
		rect(MARGIN, y, innerWidth, headerHeight, '#eff6ff');
		strokeRect(MARGIN, y, innerWidth, headerHeight, '#bfdbfe');
		let x = MARGIN;
		columns.forEach((column, index) => {
			text(column.header, x + 6, y + 8, { size: 7, bold: true, color: '#0a4fd4' });
			x += widths[index];
		});
		y += headerHeight;

		table.rows.forEach((row, rowIndex) => {
			const wrapped = row.map((cell, index) => wrapText(cell, widths[index] - 12, fontSize, 3));
			const rowHeight = Math.max(
				25,
				Math.max(...wrapped.map((lines) => lines.length)) * (fontSize + 3) + 12
			);
			ensureSpace(rowHeight);

			if (rowIndex % 2 === 0) {
				rect(MARGIN, y, innerWidth, rowHeight, '#ffffff');
			} else {
				rect(MARGIN, y, innerWidth, rowHeight, '#f8fafc');
			}
			strokeRect(MARGIN, y, innerWidth, rowHeight, '#e2e8f0');

			let cellX = MARGIN;
			wrapped.forEach((lines, columnIndex) => {
				lines.forEach((line, lineIndex) => {
					text(line, cellX + 6, y + 8 + lineIndex * (fontSize + 3), {
						size: fontSize,
						color: columnIndex === 0 ? '#0f172a' : '#475569'
					});
				});
				cellX += widths[columnIndex];
			});
			y += rowHeight;
		});
		y += 18;
	}

	drawHero();
	drawMeta();
	drawStats(report.stats ?? []);
	report.tables.forEach(drawTable);
	pages.push(commands);

	const objects: string[] = [];
	const addObject = (body: string) => {
		objects.push(body);
		return objects.length;
	};

	const catalogId = addObject('<< /Type /Catalog /Pages 2 0 R >>');
	const pagesObjectIndex = objects.length;
	addObject('');
	const fontRegularId = addObject(
		'<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>'
	);
	const fontBoldId = addObject(
		'<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>'
	);
	const pageIds: number[] = [];

	pages.forEach((pageCommands) => {
		const contentId = addObject(
			`<< /Length ${pageCommands.length} >>\nstream\n${pageCommands}endstream`
		);
		const pageId = addObject(
			`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth.toFixed(2)} ${pageHeight.toFixed(2)}] /Resources << /Font << /${FONT_REGULAR} ${fontRegularId} 0 R /${FONT_BOLD} ${fontBoldId} 0 R >> >> /Contents ${contentId} 0 R >>`
		);
		pageIds.push(pageId);
	});

	objects[pagesObjectIndex] =
		`<< /Type /Pages /Kids [${pageIds.map((id) => `${id} 0 R`).join(' ')}] /Count ${pageIds.length} >>`;

	let pdf = '%PDF-1.4\n%\xE2\xE3\xCF\xD3\n';
	const offsets = [0];
	objects.forEach((body, index) => {
		offsets.push(pdf.length);
		pdf += `${index + 1} 0 obj\n${body}\nendobj\n`;
	});
	const xrefOffset = pdf.length;
	pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
	for (let index = 1; index < offsets.length; index += 1) {
		pdf += `${offsets[index].toString().padStart(10, '0')} 00000 n \n`;
	}
	pdf += `trailer\n<< /Size ${objects.length + 1} /Root ${catalogId} 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`;

	const bytes = new Uint8Array(pdf.length);
	for (let index = 0; index < pdf.length; index += 1) {
		bytes[index] = pdf.charCodeAt(index) & 0xff;
	}

	return new Blob([bytes], { type: 'application/pdf' });
}

export function downloadPdfReport(report: PdfReportDocument, fileName: string) {
	if (typeof window === 'undefined') {
		return;
	}

	const blob = buildPdfReport(report);
	const url = window.URL.createObjectURL(blob);
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.download = `${normalizeFileName(fileName)}.pdf`;
	document.body.append(anchor);
	anchor.click();
	anchor.remove();
	window.setTimeout(() => window.URL.revokeObjectURL(url), 1000);
}
