import * as pdfjsLib from '/libman/pdfjs-dist/build/pdf.mjs';
pdfjsLib.GlobalWorkerOptions.workerSrc = '/libman/pdfjs-dist/build/pdf.worker.mjs';

const cMapBaseUrl = '/libman/pdfjs-dist/cmaps/';
const cMapPacked = true;
const { getDocument } = pdfjsLib;

// Initialize image resizing behavior on component mount
export function initialize(canvasId, imgId) {
  const outputImg = document.getElementById(imgId);
  window.addEventListener('resize', () => adjustPreview(outputImg));
  outputImg?.addEventListener('load', () => adjustPreview(outputImg));
}

// Render using a .NET stream reference for clean separation
export async function renderFirstPageFromStream(streamRef, canvasId, imgId) {
  // Acquire a ReadableStream from the .NET stream reference
  const jsStream = await streamRef.stream();
  // Convert the stream into an ArrayBuffer for PDF.js
  const arrayBuffer = await new Response(jsStream).arrayBuffer();

  const loadingTask = getDocument({ data: arrayBuffer, cMapUrl: cMapBaseUrl, cMapPacked });
  const pdf = await loadingTask.promise;
  const page = await pdf.getPage(1);
  const viewport = page.getViewport({ scale: 2 });

  const canvas = document.getElementById(canvasId);
  canvas.width = viewport.width;
  canvas.height = viewport.height;
  const ctx = canvas.getContext('2d');
  await page.render({ canvasContext: ctx, viewport }).promise;

  const outputImg = document.getElementById(imgId);
  outputImg.src = canvas.toDataURL('image/png');
  adjustPreview(outputImg);
}

function adjustPreview(outputImg) {
  if (!outputImg) return;
  const rect = outputImg.getBoundingClientRect();
  const top = rect.top + window.scrollY;
  const h = window.innerHeight - top - 20;
  outputImg.style.maxHeight = h + 'px';
}
