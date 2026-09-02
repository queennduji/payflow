# 3. Ledger balances are derived, never stored

Date: 2026-08-21

## Status

Accepted

## Context

The Ledger service needs to answer "what's this account's balance?" There are two ways to do that:
store a running balance on the `Account` row and update it inside the same transaction as each
posting, or compute the balance on read by summing every posted `LedgerLine` for that account.

## Decision

`Account` (`Payflow.Ledger.Domain.Account`) has no balance field. `GetAccountBalanceQuery` computes
the balance on every read by summing posted lines and applying the account type's normal-balance
sign (`AccountBalanceCalculator`: Asset/Expense are debit-normal, Liability/Equity/Revenue are
credit-normal).

Account provisioning is likewise simplified: accounts are auto-opened on first reference with a
type inferred from an id prefix (`merchant:` → Liability, `customer:` → Asset – see
`EfAccountRepository.InferType`) rather than requiring an explicit chart-of-accounts step.

## Consequences

A derived balance can never drift from the entries that are supposed to explain it – there is no
"balance says X but the entries sum to Y" class of bug to debug, at the cost of an aggregation
query instead of an indexed column read. At this project's scale that trade is easy; a
high-throughput ledger would typically add a periodically-reconciled balance snapshot on top of
this same source of truth rather than replace it.

The auto-provisioning convention is a demo simplification, not a real chart-of-accounts. A
production ledger would require accounts to be explicitly opened (with a real type, owner, and
currency) before anything could post to them.
