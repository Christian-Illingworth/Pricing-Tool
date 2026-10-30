# Contract: "Open in Outlook"

The app never sends or reads email (FR-056). It creates **one** draft in the signed-in agent's own
mailbox, with only the PDF attached (FR-054). See research R3.

## When it's allowed

- The PDF exists, and the user is the quote's owner.
- The PDF's `PricesFetchedOn` is today, UK time (FR-052, FR-076). Otherwise the agent sees "These prices
  have expired. Re-price and generate a new PDF first."

## Graph mode (`Outlook:Mode = Graph`)

| Item | Value |
|---|---|
| Permission | Delegated `Mail.ReadWrite` (incremental consent). No application permissions |
| Token | `ITokenAcquisition.GetAccessTokenForUserAsync(["https://graph.microsoft.com/Mail.ReadWrite"])` |
| Call | `POST https://graph.microsoft.com/v1.0/me/messages` |
| Calls never made | `/send`, `/me/mailFolders`, any `GET` on messages |

Request body:

```json
{
  "attachments": [
    {
      "@odata.type": "#microsoft.graph.fileAttachment",
      "name": "Acme_Trading_Ltd_ElectricityQuote_2026-10-30.pdf",
      "contentType": "application/pdf",
      "contentBytes": "<base64 of the stored PDF>"
    }
  ]
}
```

The body has no `subject`, `body`, `toRecipients`, `ccRecipients` or `from`. If the PDF is over 3 MB,
the app refuses with a clear message.

**Success (201)**: store `id` → `OutlookDraft.GraphMessageId` and `webLink` → `OutlookDraft.WebLink`.
The handler returns `{ "ok": true, "message": "Saved to your Outlook Drafts", "webLink": "<webLink>" }`.
The page calls:

```js
CU.toast('Saved to your Outlook Drafts', { action: { label: 'Open', onClick: () => window.open(webLink, '_blank', 'noopener') } });
```

The draft also appears in Drafts in classic and new Outlook, because it lives in the mailbox.

**Failures**:

| Cause | What the agent sees | Recorded |
|---|---|---|
| Consent needed (`MsalUiRequiredException`) | A redirect for incremental consent, returning to the quote | Nothing until it succeeds |
| 401 or 403 from Graph | "Outlook couldn't be reached. You can still download the PDF." | AuditEntry `OutlookDraftFailed` and a reporting `Failed` event |
| 429, 5xx or timeout | The same message, plus "Try again" | The same |

The Download PDF button always stays available (FR-057).

## Eml mode (`Outlook:Mode = Eml`)

This is the fallback if IT won't grant `Mail.ReadWrite`, and it is always used with the Development
test sign-in. It meets FR-054 and FR-055 only partly (research R3).

The response is `Content-Type: message/rfc822` with
`Content-Disposition: attachment; filename="<pdf base name>.eml"`:

```text
X-Unsent: 1
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="cu-<guid>"

--cu-<guid>
Content-Type: text/plain; charset=utf-8
Content-Transfer-Encoding: 7bit


--cu-<guid>
Content-Type: application/pdf; name="<file name>"
Content-Disposition: attachment; filename="<file name>"
Content-Transfer-Encoding: base64

<base64, 76-character lines>
--cu-<guid>--
```

The message has no From, To, Subject or Date headers. Line endings are CRLF.

**What the agent sees**: a toast saying "Outlook draft downloaded. Open it to address and send. In new
Outlook, if it opens read-only, attach the PDF yourself."

## Record

Every successful draft writes, in one transaction:

- an `OutlookDraft` row (who, when, method, message ID and link);
- `AuditEntry` `OutlookDraftCreated`;
- a reporting record of type `OpenedInOutlook`;
- an update to `Quote.LastActivityUtc`.

The quote page shows who created each draft and when (US4-4).
