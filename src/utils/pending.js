// "What exactly is pending" — derived, never stored.

// From the appointment row alone (list views).
export const nextStepForStatus = (status) => ({
  Requested: 'Awaiting front-desk approval',
  Scheduled: 'Awaiting check-in on the day',
  Confirmed: 'Awaiting check-in on the day',
  CheckedIn: 'With nurse / awaiting consultation',
  InConsultation: 'Doctor consulting — then complete & bill',
}[status] || null);

// From the full File (precise, multi-item, clickable).
export const pendingActions = (file) => {
  if (!file) return [];
  const items = [];
  const s = file.status;

  if (s === 'Requested') items.push({ target: 'top', label: 'Front desk: approve this request' });
  if (s === 'Scheduled' || s === 'Confirmed') items.push({ target: 'top', label: 'Front desk: check the patient in on arrival' });

  if (['CheckedIn', 'InConsultation'].includes(s)) {
    if (!file.record?.vitalSigns) items.push({ target: 'vitals', label: 'Nurse: record vitals' });
    if (!file.record?.diagnosis) items.push({ target: 'consult', label: 'Doctor: save the consultation' });
    else items.push({ target: 'top', label: 'Doctor: complete the visit to generate the bill' });
  }

  const openLabs = (file.labTests || []).filter((t) => t.status !== 'Completed' && t.status !== 'Cancelled');
  if (openLabs.length > 0) items.push({ target: 'labs', label: `Lab: ${openLabs.length} result(s) pending` });

  const p = file.prescription;
  if (p) {
    if (p.status === 'SentToPharmacy') items.push({ target: 'prescription', label: 'Pharmacy: confirm the prescription' });
    if (p.status === 'Confirmed') items.push({ target: 'prescription', label: 'Pharmacy: preparing — mark ready' });
    if (p.status === 'Ready') items.push({ target: 'prescription', label: 'Pharmacy: dispense at pickup' });
    if (p.status === 'Rejected') items.push({ target: 'prescription', label: `Prescription rejected — ${p.rejectReason || 'see reason'}` });
  }

  (file.bills || []).forEach((b) => {
    if (b.status !== 'Paid') {
      items.push({ target: 'bills', label: `Pay ${b.billType.toLowerCase()} bill #${b.billId} — ${Number(b.patientResponsibility).toFixed(2)} due` });
    }
  });

  if (items.length === 0 && ['Completed', 'Cancelled', 'No-Show'].includes(s)) {
    items.push({ target: 'top', label: s === 'Completed' ? 'All done — nothing pending on this visit' : `Visit ${s.toLowerCase()} — nothing pending` });
  }
  return items;
};
